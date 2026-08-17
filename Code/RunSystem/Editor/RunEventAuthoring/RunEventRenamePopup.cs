#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Work.PSB.Code.RunSystem.Editor
{
    //RunEvent, Talk, Choice, Action 등 선택한 SO 에셋의 새 이름을 입력받는 작은 Popup
    internal sealed class RunEventRenamePopup : PopupWindowContent
    {
        private readonly Action<string> _onConfirmed; //변경할 이름을 전달할 확정 콜백
        private string _newName; //입력 중인 새 에셋 이름
        private bool _focusRequested; //입력창에 포커스를 한 번만 주기 위한 값
		
        //현재 SO 이름을 기본값으로 넣고 이름 변경 콜백을 저장
        public RunEventRenamePopup(string currentName, Action<string> onConfirmed)
        {
            _newName = currentName ?? string.Empty;
            _onConfirmed = onConfirmed;
        }
		
        //이름 변경 Popup의 고정 크기
        public override Vector2 GetWindowSize()
        {
            return new Vector2(320f, 86f);
        }
		
        //이름 입력칸과 취소, 변경 버튼을 그림
        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("에셋 이름 변경", EditorStyles.boldLabel);
            GUI.SetNextControlName("RunEventRenameField");
            _newName = EditorGUILayout.TextField(_newName);
			
            if (!_focusRequested)
            {
                //Popup이 열린 첫 화면에서 바로 이름을 입력할 수 있도록 포커스
                EditorGUI.FocusTextInControl("RunEventRenameField");
                _focusRequested = true;
            }
			
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
			
            if (GUILayout.Button("취소", GUILayout.Width(72f)))
                editorWindow.Close();
			
            if (GUILayout.Button("변경", GUILayout.Width(72f)))
                Confirm();
			
            EditorGUILayout.EndHorizontal();
			
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.KeyDown &&
                (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter))
            {
                //일반 Enter와 숫자 키패드 Enter 모두 이름 변경으로 처리
                currentEvent.Use();
                Confirm();
            }
        }
		
        //앞뒤 공백과 빈 이름을 검사한 뒤 새 이름을 전달하고 Popup을 닫음
        private void Confirm()
        {
            string trimmedName = _newName.Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
                return;
			
            _onConfirmed?.Invoke(trimmedName);
            editorWindow.Close();
        }
		
    }
}
#endif
