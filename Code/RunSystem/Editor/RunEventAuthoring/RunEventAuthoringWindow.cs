#if UNITY_EDITOR
using System.Collections.Generic;
using PSW.Code.Talk;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.UIElements;

namespace Work.PSB.Code.RunSystem.Editor
{
    //전체 Editor 창의 조립과 명령 처리를 담당
    public class RunEventAuthoringWindow : EditorWindow
    {
        //UxmlPath, UssPath : UI 구조와 스타일 파일 경로
        private const string UxmlPath =
            "Assets/00.Work/PSB/Code/RunSystem/Editor/RunEventAuthoring/UI/RunEventAuthoringWindow.uxml";
        private const string UssPath =
            "Assets/00.Work/PSB/Code/RunSystem/Editor/RunEventAuthoring/UI/RunEventAuthoringWindow.uss";
		
        private RunEventAuthoringState _state; //공용 선택, 필터 상태
        private RunEventAssetRepository _repository; //에셋 검색과 경로 처리
        private RunEventAssetFactory _factory; //에셋 생성,복제, 연결
        private RunEventValidationService _validator; //오류 검사
        
        private RunEventBrowserView _browserView; //왼쪽 이벤트 목록
        private RunEventFlowView _flowView; //가운데 이벤트 연결 구조
        private RunEventInspectorView _inspectorView; //오른쪽 상세 설정과 검사 결과
        
        private VisualElement _windowRoot;
        private VisualElement _flowPane; //반응형 크기 감지 대상
        
        private Label _statusLabel;
        private Label _pathLabel; //하단 상태 표시
        
        private Button _renameButton; //상단 RunEvent 이름 변경 Popup 위치 계산에 사용
        private Button _renameSelectedButton; //오른쪽에서 선택한 SO 이름 변경 Popup 위치 계산에 사용
        private bool _refreshScheduled; //중복 새로고침 예약 방지
		
        //Tools/PSB/Run Event Editor 메뉴, 최소 크기는 820×540
        [MenuItem("Tools/PSB/Run Event Editor")]
        public static void OpenWindow()
        {
            RunEventAuthoringWindow window = GetWindow<RunEventAuthoringWindow>();
            window.titleContent = new GUIContent("Run Event Editor");
            window.minSize = new Vector2(820f, 540f);
            window.Show();
        }
		
        //Project 변경, Undo와 Unity 선택 변경 이벤트를 구독
        private void OnEnable()
        {
            EditorApplication.projectChanged += ScheduleRefresh;
            Undo.undoRedoPerformed += ScheduleRefresh;
            Selection.selectionChanged += HandleUnitySelectionChanged;
        }
		
        //창이 닫힐 때 Editor 이벤트와 View의 등록 내용을 정리
        private void OnDisable()
        {
            EditorApplication.projectChanged -= ScheduleRefresh;
            Undo.undoRedoPerformed -= ScheduleRefresh;
            Selection.selectionChanged -= HandleUnitySelectionChanged;
            DisposeViews();
        }
		
        //UXML과 USS를 불러오고 Editor에서 사용할 모든 기능 객체를 연결
        public void CreateGUI()
        {
            DisposeViews();
            rootVisualElement.Clear(); //기존 View 정리, 기존 UI 제거
			
            VisualTreeAsset visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
            //UXML과 USS 로드
			
            if (visualTree == null)
            {
                rootVisualElement.Add(new HelpBox(
                    $"Run Event Editor UXML을 찾을 수 없습니다.\n{UxmlPath}", HelpBoxMessageType.Error));
                return; //UXML이 없으면 오류 HelpBox 표시
            }
			
            visualTree.CloneTree(rootVisualElement); //UXML 복제
            if (styleSheet != null)
                rootVisualElement.styleSheets.Add(styleSheet); //USS 적용
			
            _windowRoot = rootVisualElement.Q<VisualElement>("window-root");
            _flowPane = rootVisualElement.Q<VisualElement>("flow-pane");
            RegisterResponsiveLayout(); //반응형 크기 이벤트 등록
			
            _state = new RunEventAuthoringState();
            _repository = new RunEventAssetRepository();
            _factory = new RunEventAssetFactory(_repository);
            _validator = new RunEventValidationService(_repository);
            //State, Repository, Factory, Validator 생성
            
            _browserView = new RunEventBrowserView(rootVisualElement, _state, _repository, _validator);
            _flowView = new RunEventFlowView(rootVisualElement, _state,
                _repository, _factory, HandleAssetsChanged);
            _inspectorView = new RunEventInspectorView(rootVisualElement, _state, _repository);
            //Browser, Flow, Inspector View 생성
            
            _statusLabel = rootVisualElement.Q<Label>("status-label");
            _pathLabel = rootVisualElement.Q<Label>("path-label");
            _renameButton = rootVisualElement.Q<Button>("rename-event-button");
            _renameSelectedButton = rootVisualElement.Q<Button>("rename-selected-button");
            //하단 상태 Label과 두 종류의 이름 변경 Popup을 열 Button을 찾음
            
            ConfigureToolbar();
            RegisterStateCallbacks();
            RefreshAll();
            HandleUnitySelectionChanged();
            //상태 이벤트 연결, 전체 새로고침, 현재 Unity Selection 반영
        }
		
        //상단 툴바 버튼과 복제 메뉴에 실행할 기능을 연결
        private void ConfigureToolbar()
        {
            rootVisualElement.Q<Button>("new-event-button").clicked += ShowNewEventMenu; //템플릿 메뉴 표시
            _renameButton.clicked += ShowEventRenamePopup; //기준 RunEvent 이름 변경 Popup 표시
            _renameSelectedButton.clicked += ShowSelectedObjectRenamePopup; //현재 상세 SO 이름 변경 Popup 표시
            rootVisualElement.Q<Button>("reveal-button").clicked += RevealSelectedObject; //선택 SO Ping
            rootVisualElement.Q<Button>("refresh-button").clicked += RefreshAll; //모든 View 재구성
            rootVisualElement.Q<Button>("save-button").clicked += SaveAssets; //AssetDatabase 저장
            rootVisualElement.Q<Button>("validate-button").clicked += ValidateAll; //모든 이벤트 검사
			
            ToolbarMenu duplicateMenu = rootVisualElement.Q<ToolbarMenu>("duplicate-event-menu");
            duplicateMenu.menu.AppendAction("얕은 복제 (연결 공유)",
                _ => DuplicateSelectedEvent(false), GetDuplicateMenuStatus);
            duplicateMenu.menu.AppendAction("깊은 복제 (연결 SO 포함)",
                _ => DuplicateSelectedEvent(true), GetDuplicateMenuStatus);
            //연결 공유 또는 연결 SO 포함 복제
        }
		
        //기준 이벤트와 상세 SO 선택 변경 시 Window가 처리할 기능을 연결
        private void RegisterStateCallbacks()
        {
            _state.EventSelectionChanged += HandleEventSelectionChanged;
            _state.ObjectSelectionChanged += UpdatePathLabel;
            _state.ObjectSelectionChanged += UpdateDetailHeaderButtons;
        }
		
        //Window와 각 View가 등록한 이벤트를 해제하고 View 참조를 정리
        private void DisposeViews()
        {
            UnregisterResponsiveLayout();
			
            if (_state != null)
            {
                _state.EventSelectionChanged -= HandleEventSelectionChanged;
                _state.ObjectSelectionChanged -= UpdatePathLabel;
                _state.ObjectSelectionChanged -= UpdateDetailHeaderButtons;
            }
			
            _browserView?.Dispose();
            _flowView?.Dispose();
            _inspectorView?.Dispose();
            _browserView = null;
            _flowView = null;
            _inspectorView = null;
        }
		
        //전체 창과 가운데 Flow의 크기 변경 이벤트를 등록
        private void RegisterResponsiveLayout()
        {
            _windowRoot?.RegisterCallback<GeometryChangedEvent>(HandleWindowGeometryChanged);
            _flowPane?.RegisterCallback<GeometryChangedEvent>(HandleFlowGeometryChanged);
        }
		
        //크기 변경 이벤트를 해제하고 UI 요소 참조를 정리
        private void UnregisterResponsiveLayout()
        {
            _windowRoot?.UnregisterCallback<GeometryChangedEvent>(HandleWindowGeometryChanged);
            _flowPane?.UnregisterCallback<GeometryChangedEvent>(HandleFlowGeometryChanged);
            _windowRoot = null;
            _flowPane = null;
        }
		
        //전체 창 너비에 따라 compact와 narrow USS 클래스를 적용
        private void HandleWindowGeometryChanged(GeometryChangedEvent evt)
        {
            float width = evt.newRect.width;
            _windowRoot.EnableInClassList("compact-window", width > 0f && width < 1180f); //창 너비 1180 미만
            _windowRoot.EnableInClassList("narrow-window", width > 0f && width < 900f); //창 너비 900 미만
        }
		
        //가운데 Flow 너비에 따라 세로 연결 배치용 USS 클래스를 적용
        private void HandleFlowGeometryChanged(GeometryChangedEvent evt)
        {
            float width = evt.newRect.width;
            _flowPane.EnableInClassList("compact-flow", width > 0f && width < 620f); //Flow 너비 620 미만
        }
		//EnableInClassList()가 USS 클래스를 켜거나 끄면서 화면 배치가 변경됨
        
        //템플릿 목록 생성
        private void ShowNewEventMenu()
        {
            GenericMenu menu = new GenericMenu();
            AddTemplateMenuItem(menu, "빈 이벤트", RunEventTemplateKind.Empty);
            AddTemplateMenuItem(menu, "대화 이벤트", RunEventTemplateKind.Dialogue);
            AddTemplateMenuItem(menu, "선택지 이벤트", RunEventTemplateKind.Choice);
            menu.AddSeparator(string.Empty);
            
            AddTemplateMenuItem(menu, "대화 후 전투", RunEventTemplateKind.Battle);
            AddTemplateMenuItem(menu, "제한 턴 피해 Challenge", RunEventTemplateKind.DamageChallenge);
            AddTemplateMenuItem(menu, "체력 1 종료 전투", RunEventTemplateKind.HealthOneBattle);
            AddTemplateMenuItem(menu, "체력 퍼센트 종료 전투", RunEventTemplateKind.HealthPercentBattle);
            menu.AddSeparator(string.Empty);
            
            AddTemplateMenuItem(menu, "자원 교환 이벤트", RunEventTemplateKind.Transaction);
            AddTemplateMenuItem(menu, "확률 이벤트", RunEventTemplateKind.Chance);
            AddTemplateMenuItem(menu, "더블 오어 낫싱", RunEventTemplateKind.DoubleOrNothing);
            menu.ShowAsContext();
        }
		
        //선택한 템플릿 생성 후 목록 갱신, 새 이벤트 선택, Project 위치 표시
        private void AddTemplateMenuItem(GenericMenu menu, string label, RunEventTemplateKind templateKind)
        {
            menu.AddItem(new GUIContent(label), false, () =>
            {
                RunEventSO createdEvent = _factory.CreateRunEvent(templateKind);
                if (createdEvent == null)
                    return;
				
                RefreshAll();
                _state.SelectEvent(createdEvent);
                _repository.Reveal(createdEvent);
                SetStatus($"'{createdEvent.name}' 이벤트를 생성했습니다.");
            });
        }
		
        //선택 이벤트 복제 후 복제본 선택
        private void DuplicateSelectedEvent(bool duplicateConnectedAssets)
        {
            RunEventSO source = _state.SelectedEvent;
            if (source == null)
                return;
			
            RunEventSO duplicate = _factory.DuplicateEvent(source, duplicateConnectedAssets);
            if (duplicate == null)
                return;
			
            RefreshAll();
            _state.SelectEvent(duplicate);
            _repository.Reveal(duplicate);
            SetStatus(duplicateConnectedAssets 
                ? $"'{source.name}'의 연결 SO까지 깊은 복제했습니다." : $"'{source.name}'을 연결 공유 방식으로 복제했습니다.");
        }
		
        //선택 이벤트가 없으면 복제 메뉴 비활성화
        private DropdownMenuAction.Status GetDuplicateMenuStatus(DropdownMenuAction action)
        {
            return _state != null && _state.SelectedEvent != null
                ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
        }
		
        //상단 이름 변경 버튼을 기준으로 왼쪽에서 선택한 RunEvent의 이름 변경 Popup 표시
        private void ShowEventRenamePopup()
        {
            RunEventSO selectedEvent = _state.SelectedEvent;
            if (selectedEvent == null)
                return;
			
            ShowAssetRenamePopup(selectedEvent, _renameButton, "이벤트");
        }
		
        //오른쪽 이름 변경 버튼을 기준으로 현재 Inspector에 표시된 SO의 이름 변경 Popup 표시
        private void ShowSelectedObjectRenamePopup()
        {
            Object selectedObject = _state.SelectedObject;
            if (selectedObject == null)
                return;
			
            ShowAssetRenamePopup(selectedObject, _renameSelectedButton, "SO");
        }
		
        //RunEvent, Talk, Choice, Action 등 선택한 에셋에 공통 이름 변경 Popup을 표시
        private void ShowAssetRenamePopup(Object target, VisualElement anchor, string targetLabel)
        {
            if (target == null || anchor == null)
                return;
			
            string previousName = target.name; //변경 완료 메시지에 사용할 기존 이름 보관
            UnityEditor.PopupWindow.Show(anchor.worldBound,
                new RunEventRenamePopup(previousName, newName =>
                {
                    if (!_factory.RenameAsset(target, newName))
                        return;
					
                    RefreshAll(); //왼쪽 목록, 가운데 흐름, 오른쪽 Inspector 이름을 모두 다시 표시
                    SetStatus($"{targetLabel} 이름을 '{previousName}'에서 '{target.name}'으로 변경했습니다.");
                }));
        }
		
        //현재 상세 SO 또는 기준 RunEvent를 Project 창에 표시
        private void RevealSelectedObject()
        {
            Object target = _state.SelectedObject != null
                ? _state.SelectedObject : _state.SelectedEvent;
            _repository.Reveal(target);
        }
		
        //현재까지 변경된 모든 에셋을 저장
        private void SaveAssets()
        {
            AssetDatabase.SaveAssets();
            SetStatus("변경된 에셋을 저장했습니다.");
        }
		
        //모든 이벤트 검사 후 오류, 경고 개수를 상태 표시줄에 출력
        private void ValidateAll()
        {
            IReadOnlyList<RunEventValidationIssue> issues =
                _validator.ValidateAll(_browserView.AllEvents);
            _inspectorView.ShowIssues(issues);
            
            int errorCount = CountIssues(issues, RunEventValidationSeverity.Error);
            int warningCount = CountIssues(issues, RunEventValidationSeverity.Warning);
            SetStatus($"전체 검사 완료: 오류 {errorCount}개, 경고 {warningCount}개");
        }
		
        //현재 이벤트만 검사
        private void ValidateSelectedEvent()
        {
            IReadOnlyList<RunEventValidationIssue> issues =
                _validator.ValidateEvent(_state.SelectedEvent);
            _inspectorView.ShowIssues(issues);
        }
		
        //Browser, Flow, Inspector, 검사 결과, 경로를 모두 갱신
        private void RefreshAll()
        {
            if (_browserView == null)
                return;
			
            _browserView.Refresh();
            _flowView.Refresh();
            _inspectorView.RefreshInspector();
            ValidateSelectedEvent();
            UpdatePathLabel();
            UpdateDetailHeaderButtons();
            SetStatus("에셋 목록을 새로고침했습니다.");
        }
		
        //Flow에서 SO가 생성, 연결됐을 때 관련 영역만 갱신
        private void HandleAssetsChanged()
        {
            _browserView.Refresh();
            _flowView.Refresh();
            ValidateSelectedEvent();
            UpdatePathLabel();
        }
		
        //기준 RunEvent가 바뀌면 검사 결과와 하단 경로를 갱신
        private void HandleEventSelectionChanged()
        {
            ValidateSelectedEvent();
            UpdatePathLabel();
        }
		
        //Project 창의 선택을 Run Event Editor의 선택 상태에 반영
        private void HandleUnitySelectionChanged()
        {
            if (_state == null)
                return;
			
            switch (Selection.activeObject)
            {
                case RunEventSO eventData:
                    _state.SelectEvent(eventData);
                    break;
                case TalkDataListSO _:
                case TalkChoiceDataSO _:
                case RunEventActionSO _:
                    _state.SelectObject(Selection.activeObject);
                    break;
            }
        }
		
        //같은 프레임에 여러 변경 알림이 와도 다음 Editor tick에 한 번만 새로고침
        private void ScheduleRefresh()
        {
            if (_refreshScheduled)
                return;
			
            _refreshScheduled = true;
            EditorApplication.delayCall += () =>
            {
                _refreshScheduled = false;
                if (this != null && rootVisualElement != null)
                    RefreshAll();
            };
        }
		
        //현재 상세 SO 또는 기준 RunEvent의 에셋 경로를 하단에 표시
        private void UpdatePathLabel()
        {
            if (_pathLabel == null || _repository == null || _state == null)
                return;
			
            UnityEngine.Object target = _state.SelectedObject != null
                ? _state.SelectedObject : _state.SelectedEvent;
            _pathLabel.text = _repository.GetAssetPath(target);
        }
		
        //오른쪽에 실제 Project 에셋이 선택됐을 때만 SO 이름 변경 버튼을 활성화
        private void UpdateDetailHeaderButtons()
        {
            if (_renameSelectedButton == null || _state == null)
                return;
			
            Object target = _state.SelectedObject;
            bool canRename = target != null && !string.IsNullOrWhiteSpace(AssetDatabase.GetAssetPath(target));
            _renameSelectedButton.SetEnabled(canRename);
        }
		
        //최근 실행한 작업 내용을 하단 상태 Label에 표시
        private void SetStatus(string message)
        {
            if (_statusLabel != null)
                _statusLabel.text = message;
        }
		
        //검사 결과에서 지정한 심각도와 같은 항목의 개수를 계산
        private static int CountIssues(IReadOnlyList<RunEventValidationIssue> issues,
            RunEventValidationSeverity severity)
        {
            int count = 0;
			
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == severity)
                    count++;
            }
			
            return count;
        }
		
    }
}
#endif
