#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Work.PSB.Code.RunSystem.Editor
{
    //선택한 SO의 Inspector와 검사 결과를 오른쪽 화면에 표시
    public class RunEventInspectorView : IDisposable
    {
        private readonly RunEventAuthoringState _state; //오른쪽 Inspector에 표시할 현재 선택 상태
        private readonly RunEventAssetRepository _repository; //선택한 에셋의 Project 위치 표시
        private readonly Label _titleLabel; //현재 상세 대상의 이름
        private readonly ScrollView _inspectorScroll; //Unity Inspector가 들어갈 영역
        private readonly ListView _validationList; //오류, 경고, 안내 목록
        private readonly Label _validationCountLabel; //오류와 경고 개수
        private readonly List<RunEventValidationIssue> _issues = new List<RunEventValidationIssue>(); //현재 표시할 검사 결과
        private UnityEditor.Editor _editor; //현재 선택 SO를 그리는 Unity Editor
		
        //UXML 요소를 찾아 Inspector와 검사 목록을 구성
        public RunEventInspectorView(VisualElement root, RunEventAuthoringState state,
            RunEventAssetRepository repository)
        {
            _state = state;
            _repository = repository;
            _titleLabel = root.Q<Label>("selected-object-title");
            _inspectorScroll = root.Q<ScrollView>("inspector-scroll");
            _inspectorScroll.mode = ScrollViewMode.Vertical;
            _inspectorScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _validationList = root.Q<ListView>("validation-list");
            _validationCountLabel = root.Q<Label>("validation-count-label");
			
            ConfigureValidationList();
            root.Q<Button>("ping-selected-button").clicked += PingSelectedObject;
            _state.ObjectSelectionChanged += RefreshInspector;
            RefreshInspector();
        }
		
        //등록한 선택 이벤트와 생성된 Unity Editor를 정리
        public void Dispose()
        {
            _state.ObjectSelectionChanged -= RefreshInspector;
            DestroyEditor();
        }
		
        //검사 결과를 심각도와 메시지 순서로 정렬해 ListView에 표시
        public void ShowIssues(IEnumerable<RunEventValidationIssue> issues)
        {
            _issues.Clear();
			
            if (issues != null)
            {
                _issues.AddRange(issues
                    .OrderByDescending(issue => issue.Severity)
                    .ThenBy(issue => issue.Message, StringComparer.Ordinal));
            }
			
            _validationList.Rebuild();
            int errorCount = _issues.Count(issue => issue.Severity == RunEventValidationSeverity.Error);
            int warningCount = _issues.Count(issue => issue.Severity == RunEventValidationSeverity.Warning);
            _validationCountLabel.text = errorCount > 0 ? $"{errorCount}E {warningCount}W" : warningCount.ToString();
        }
		
        //선택된 SO에 맞는 Unity Inspector를 오른쪽 영역에 새로 생성
        public void RefreshInspector()
        {
            DestroyEditor();
            _inspectorScroll.Clear();
            Object target = _state.SelectedObject;
			
            if (target == null)
            {
                _titleLabel.text = "상세 설정";
                Label emptyLabel = new Label("이벤트 흐름에서 편집할 SO를 선택하세요.");
                emptyLabel.AddToClassList("inspector-empty");
                _inspectorScroll.Add(emptyLabel);
                return;
            }
			
            _titleLabel.text = target.name;
            _editor = UnityEditor.Editor.CreateEditor(target);
            //RunEventSO면 RunEventSOEditor, 그 외 SO면 해당 타입의 Editor가 자동으로 생성
			
            if (_editor == null)
            {
                Label errorLabel = new Label("Inspector를 생성할 수 없습니다.");
                errorLabel.AddToClassList("inspector-empty");
                _inspectorScroll.Add(errorLabel);
                return;
            }
			
            InspectorElement inspector = new InspectorElement(_editor);
            inspector.style.flexGrow = 1f;
            _inspectorScroll.Add(inspector);
        }
		
        //검사 결과 ListView의 데이터, 높이, 생성과 연결 방식을 설정
        private void ConfigureValidationList()
        {
            _validationList.itemsSource = _issues;
            _validationList.fixedItemHeight = 48f;
            _validationList.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            _validationList.selectionType = SelectionType.Single;
            _validationList.makeItem = MakeValidationRow;
            _validationList.bindItem = BindValidationRow;
            _validationList.selectionChanged += HandleValidationSelectionChanged;
        }
		
        //재사용할 검사 결과 한 줄의 모양을 생성
        private static VisualElement MakeValidationRow()
        {
            VisualElement row = new VisualElement();
            row.AddToClassList("validation-row");
            Label mark = new Label { name = "mark" };
            mark.AddToClassList("validation-mark");
            Label message = new Label { name = "message" };
            message.AddToClassList("validation-message");
            row.Add(mark);
            row.Add(message);
            return row;
        }
		
        //검사 결과의 심각도에 따라 E, W, I와 색상 클래스를 연결
        private void BindValidationRow(VisualElement row, int index)
        {
            if (index < 0 || index >= _issues.Count)
                return;
			
            RunEventValidationIssue issue = _issues[index];
            row.RemoveFromClassList("validation-error");
            row.RemoveFromClassList("validation-warning");
            row.RemoveFromClassList("validation-info");
			
            string mark;
            switch (issue.Severity)
            {
                case RunEventValidationSeverity.Error:
                    mark = "E";
                    row.AddToClassList("validation-error");
                    break;
                case RunEventValidationSeverity.Warning:
                    mark = "W";
                    row.AddToClassList("validation-warning");
                    break;
                default:
                    mark = "I";
                    row.AddToClassList("validation-info");
                    break;
            }
			
            row.Q<Label>("mark").text = mark;
            row.Q<Label>("message").text = issue.Message;
            row.tooltip = issue.Target != null ? $"{issue.Message}\n대상: {issue.Target.name}" : issue.Message;
        }
		
        //검사 결과를 클릭하면 원본 이벤트와 실제 문제 SO를 선택
        private void HandleValidationSelectionChanged(IEnumerable<object> selectedItems)
        {
            RunEventValidationIssue issue = selectedItems.OfType<RunEventValidationIssue>().FirstOrDefault();
            if (issue == null)
                return;
			
            if (issue.OwnerEvent != null)
                _state.SelectEvent(issue.OwnerEvent);
			
            Object target = issue.Target != null ? issue.Target : issue.OwnerEvent;
            _state.SelectObject(target);
            _repository.Reveal(target);
        }
		
        //현재 상세 SO를 Project 창에서 선택하고 위치를 표시
        private void PingSelectedObject()
        {
            _repository.Reveal(_state.SelectedObject);
        }
		
        //이전에 생성한 Unity Editor를 즉시 제거해 남아 있지 않도록 정리
        private void DestroyEditor()
        {
            if (_editor == null)
                return;
			
            Object.DestroyImmediate(_editor);
            _editor = null;
        }
		
    }
}
#endif
