#if UNITY_EDITOR
using System;
using Object = UnityEngine.Object;

namespace Work.PSB.Code.RunSystem.Editor
{
    //여러 View가 공유하는 현재 UI 상태
    public class RunEventAuthoringState
    {
        public RunEventSO SelectedEvent { get; private set; } //왼쪽 목록에서 선택한 기준 RunEvent
        public Object SelectedObject { get; private set; } //오른쪽 Inspector에 표시할 실제 SO
        public string SearchText { get; private set; } = string.Empty; //검색어
        public bool BattleOnly { get; private set; } //전투 이벤트만 표시할지
        public bool IssuesOnly { get; private set; } //검사 문제가 있는 이벤트만 표시할지
		
        public event Action EventSelectionChanged; //기준 RunEvent가 변경됨
        public event Action ObjectSelectionChanged; //오른쪽 Inspector 대상이 변경됨
        public event Action FilterChanged; //검색어나 필터가 변경됨
		
        //기준 이벤트와 상세 대상을 모두 RunEvent로 설정하고 두 변경 이벤트를 호출
        public void SelectEvent(RunEventSO eventData)
        {
            if (SelectedEvent == eventData)
                return;
			
            SelectedEvent = eventData;
            SelectedObject = eventData;
            EventSelectionChanged?.Invoke();
            ObjectSelectionChanged?.Invoke();
        }
		
        //상세 대상만 바꾸고 기준 이벤트는 유지
        public void SelectObject(Object target)
        {
            if (SelectedObject == target)
                return;
			
            SelectedObject = target;
            ObjectSelectionChanged?.Invoke();
        }
		
        //null을 빈 문자열로 바꾸고 필터 변경 알림을 보냄
        public void SetSearchText(string searchText)
        {
            string nextValue = searchText ?? string.Empty;
            if (SearchText == nextValue)
                return;
			
            SearchText = nextValue;
            FilterChanged?.Invoke();
        }
		
        //실제 값이 바뀔 때만 필터 변경 알림을 보냄
        public void SetBattleOnly(bool value)
        {
            if (BattleOnly == value)
                return;
			
            BattleOnly = value;
            FilterChanged?.Invoke();
        }
		
        //문제 있는 이벤트만 보기 값이 바뀔 때 필터 변경 알림을 보냄
        public void SetIssuesOnly(bool value)
        {
            if (IssuesOnly == value)
                return;
			
            IssuesOnly = value;
            FilterChanged?.Invoke();
        }
		
    }
}
#endif
