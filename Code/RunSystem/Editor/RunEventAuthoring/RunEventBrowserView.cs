#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Work.PSB.Code.RunSystem.Editor
{
    //왼쪽 이벤트 검색,필터,목록을 담당
    public class RunEventBrowserView : IDisposable
    {
        private readonly RunEventAuthoringState _state; //현재 선택, 검색어, 필터 상태
        private readonly RunEventAssetRepository _repository; //RunEventSO 목록 검색
        private readonly RunEventValidationService _validator; //이벤트별 문제 개수 검사
        
        private readonly ToolbarSearchField _searchField; //이름과 Talk ID 검색창
        private readonly Toggle _battleFilterToggle; //전투 이벤트만 표시하는 필터
        private readonly Toggle _issueFilterToggle; //문제가 있는 이벤트만 표시하는 필터
        private readonly ListView _listView; //필터를 통과한 이벤트 목록
        private readonly Label _countLabel; //현재 표시 개수와 전체 개수
        
        private readonly List<RunEventSO> _allEvents = new List<RunEventSO>(); //Repository에서 찾은 전체 이벤트
        private readonly List<RunEventSO> _visibleEvents = new List<RunEventSO>(); //현재 필터를 통과한 이벤트
        private readonly Dictionary<RunEventSO, int> _issueCounts = new Dictionary<RunEventSO, int>(); //이벤트별 Error와 Warning 개수
		
        public IReadOnlyList<RunEventSO> AllEvents => _allEvents; //전체 검사에서 사용할 읽기 전용 목록
		
        //UXML 요소를 찾아 목록과 검색, 필터 동작을 연결
        public RunEventBrowserView(VisualElement root, RunEventAuthoringState state,
            RunEventAssetRepository repository, RunEventValidationService validator)
        {
            _state = state;
            _repository = repository;
            _validator = validator;
            _searchField = root.Q<ToolbarSearchField>("event-search-field");
            _battleFilterToggle = root.Q<Toggle>("battle-filter-toggle");
            _issueFilterToggle = root.Q<Toggle>("issue-filter-toggle");
            _listView = root.Q<ListView>("event-list");
            _countLabel = root.Q<Label>("event-count-label");
			
            ConfigureListView();
            RegisterCallbacks();
        }
		
        //State에 등록한 필터와 선택 변경 이벤트를 해제
        public void Dispose()
        {
            _state.FilterChanged -= ApplyFilter;
            _state.EventSelectionChanged -= SyncSelection;
        }
		
        //RunEventSO를 다시 검색하고 문제 개수와 필터 결과를 갱신
        public void Refresh()
        {
            RunEventSO previousSelection = _state.SelectedEvent;
            _allEvents.Clear();
            _allEvents.AddRange(_repository.FindRunEvents());
            RebuildIssueCounts();
            ApplyFilter();
			
            if (previousSelection != null && _allEvents.Contains(previousSelection))
                SyncSelection();
        }
		
        //이벤트에 저장된 Error와 Warning 개수를 반환
        public int GetIssueCount(RunEventSO eventData)
        {
            return eventData != null && _issueCounts.TryGetValue(eventData, out int count) ? count : 0;
        }
		
        //ListView의 높이, 선택 방식과 행 생성, 데이터 연결 방식을 설정
        private void ConfigureListView()
        {
            _listView.itemsSource = _visibleEvents;
            _listView.fixedItemHeight = 54f;
            _listView.virtualizationMethod = CollectionVirtualizationMethod.FixedHeight; //화면에 필요한 행만 재사용
            _listView.selectionType = SelectionType.Single;
            _listView.makeItem = MakeEventRow;
            _listView.bindItem = BindEventRow;
            _listView.selectionChanged += HandleSelectionChanged;
        }
		
        //검색, 필터와 State 변경 이벤트에 실행할 메서드를 연결
        private void RegisterCallbacks()
        {
            _searchField.RegisterValueChangedCallback(evt => _state.SetSearchText(evt.newValue));
            _battleFilterToggle.RegisterValueChangedCallback(evt => _state.SetBattleOnly(evt.newValue));
            _issueFilterToggle.RegisterValueChangedCallback(evt => _state.SetIssuesOnly(evt.newValue));
            _state.FilterChanged += ApplyFilter;
            _state.EventSelectionChanged += SyncSelection;
        }
		
        //재사용할 이벤트 목록 한 줄의 이미지, 이름, ID, 문제 배지를 생성
        private VisualElement MakeEventRow()
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("event-row");
			
            Image thumbnail = new Image
            {
                name = "thumbnail",
                scaleMode = ScaleMode.ScaleToFit
            };
            thumbnail.AddToClassList("event-thumbnail");
            row.Add(thumbnail);
			
            VisualElement textRoot = new VisualElement();
            textRoot.AddToClassList("event-row-text");
            Label nameLabel = new Label { name = "name" };
            nameLabel.AddToClassList("event-row-name");
            Label idLabel = new Label { name = "id" };
            idLabel.AddToClassList("event-row-id");
            textRoot.Add(nameLabel);
            textRoot.Add(idLabel);
            row.Add(textRoot);
			
            Label issueBadge = new Label { name = "issues" };
            issueBadge.AddToClassList("issue-badge");
            row.Add(issueBadge);
            return row;
        }
		
        //목록 행에 실제 RunEventSO의 이미지, 이름, ID와 문제 개수를 입력
        private void BindEventRow(VisualElement row, int index)
        {
            if (index < 0 || index >= _visibleEvents.Count)
                return;
			
            RunEventSO eventData = _visibleEvents[index];
            string eventName = eventData != null ? eventData.name : "Missing Event";
            string eventId = eventData != null
                ? $"{eventData.TalkId}  |  {(eventData.HasBattle ? "Battle" : "Talk")}" : string.Empty;
			
            row.Q<Image>("thumbnail").sprite = eventData != null ? eventData.sprite : null;
            row.Q<Label>("name").text = eventName;
            row.Q<Label>("name").tooltip = eventName;
            row.Q<Label>("id").text = eventId;
            row.Q<Label>("id").tooltip = eventId;
			
            int issueCount = GetIssueCount(eventData);
            Label issueBadge = row.Q<Label>("issues");
            issueBadge.text = issueCount.ToString();
            issueBadge.style.display = issueCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }
		
        //목록에서 선택한 RunEventSO를 공용 State에 전달
        private void HandleSelectionChanged(IEnumerable<object> selectedItems)
        {
            RunEventSO selectedEvent = selectedItems.OfType<RunEventSO>().FirstOrDefault();
            if (selectedEvent != null)
                _state.SelectEvent(selectedEvent);
        }
		
        //전체 이벤트 중 현재 검색어와 필터를 통과한 이벤트만 목록에 표시
        private void ApplyFilter()
        {
            _visibleEvents.Clear();
			
            for (int i = 0; i < _allEvents.Count; i++)
            {
                RunEventSO eventData = _allEvents[i];
                if (!MatchesFilter(eventData))
                    continue;
				
                _visibleEvents.Add(eventData);
            }
			
            _listView.Rebuild();
            _countLabel.text = $"{_visibleEvents.Count} / {_allEvents.Count} events";
            SyncSelection();
        }
		
        //이벤트 하나가 전투, 문제, 검색어 조건을 모두 만족하는지 확인
        private bool MatchesFilter(RunEventSO eventData)
        {
            if (eventData == null)
                return false;
			
            if (_state.BattleOnly && !eventData.HasBattle)
                return false;
			
            if (_state.IssuesOnly && GetIssueCount(eventData) == 0)
                return false;
			
            if (string.IsNullOrWhiteSpace(_state.SearchText))
                return true;
			
            return eventData.name.IndexOf(_state.SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   eventData.TalkId.IndexOf(_state.SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }
		
        //각 이벤트를 검사하고 Info를 제외한 Error와 Warning 개수를 저장
        private void RebuildIssueCounts()
        {
            _issueCounts.Clear();
			
            for (int i = 0; i < _allEvents.Count; i++)
            {
                RunEventSO eventData = _allEvents[i];
                int count = _validator.ValidateEvent(eventData).Count(issue =>
                    issue.Severity != RunEventValidationSeverity.Info);
                _issueCounts[eventData] = count;
            }
        }
		
        //State에서 선택된 이벤트와 ListView의 선택 행을 동일하게 맞춤
        private void SyncSelection()
        {
            int index = _visibleEvents.IndexOf(_state.SelectedEvent);
            if (index >= 0)
                _listView.SetSelection(index);
            else
                _listView.ClearSelection();
        }
		
    }
}
#endif
