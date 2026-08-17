#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Work.PSB.Code.RunSystem.Editor
{
    //RunEvent 관련 에셋의 표준 폴더, 검색과 Project 창 표시를 담당
    public class RunEventAssetRepository
    {
        //RunEvent, 시작 대화, Choice, 다음 대화, 전투 후 대화, Action이 저장될 표준 경로를 정의
        public const string RootFolder = "Assets/00.Work/PSB/08.SO";
        public const string RunEventFolder = RootFolder + "/RunMaps/RunEvents";
        public const string TalkRootFolder = RootFolder + "/TalkList/RunTalk";
        public const string MainTalkFolder = TalkRootFolder + "/00_TalkData";
        public const string ChoiceFolder = TalkRootFolder + "/TalkChoice";
        public const string NextTalkFolder = TalkRootFolder + "/NextTalkData";
        public const string AfterBattleFolder = TalkRootFolder + "/AfterBattleData";
        public const string ActionFolder = TalkRootFolder + "/ActionData";
		
        //RunEvent를 찾고 이름 기준 대소문자 무시 정렬
        public IReadOnlyList<RunEventSO> FindRunEvents()
        {
            return FindAssets<RunEventSO>(RunEventFolder)
                .OrderBy(eventData => eventData.name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
		
        //RunTalk 아래의 모든 TalkData 검색
        public IReadOnlyList<TalkDataListSO> FindTalkData()
        {
            return FindAssets<TalkDataListSO>(TalkRootFolder).ToList();
        }
		
        //Action 폴더의 모든 RunEventAction 검색
        public IReadOnlyList<RunEventActionSO> FindActions()
        {
            return FindAssets<RunEventActionSO>(ActionFolder).ToList();
        }
		
        //GUID 검색 → 경로 변환 → 실제 에셋 로드 과정의 공통 구현
        public IReadOnlyList<T> FindAssets<T>(string folder) where T : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(folder) || !AssetDatabase.IsValidFolder(folder))
                return Array.Empty<T>();
			
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder });
            List<T> assets = new List<T>(guids.Length);
			
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    assets.Add(asset);
            }
			
            return assets;
        }
		
        //필요한 표준 폴더를 모두 보장
        public void EnsureDefaultFolders()
        {
            EnsureFolder(RunEventFolder);
            EnsureFolder(MainTalkFolder);
            EnsureFolder(ChoiceFolder);
            EnsureFolder(NextTalkFolder);
            EnsureFolder(AfterBattleFolder);
            EnsureFolder(ActionFolder);
        }
		
        //대상 에셋을 Unity Selection으로 지정하고 Project 창에서 Ping
        public void Reveal(Object target)
        {
            if (target == null)
                return;
			
            Selection.activeObject = target;
            EditorGUIUtility.PingObject(target);
        }
		
        //선택 대상 경로를 반환하며 대상이 없으면 루트 경로를 반환
        public string GetAssetPath(Object target)
        {
            return target != null ? AssetDatabase.GetAssetPath(target) : RootFolder;
        }
		
        //파일명 금지 문자를 _로 바꾸고 빈 이름이면 fallback을 사용
        public static string SanitizeAssetName(string value, string fallbackName)
        {
            string fileName = string.IsNullOrWhiteSpace(value) ? fallbackName : value.Trim();
            char[] invalidCharacters = Path.GetInvalidFileNameChars();
			
            for (int i = 0; i < invalidCharacters.Length; i++)
                fileName = fileName.Replace(invalidCharacters[i], '_');
			
            return string.IsNullOrWhiteSpace(fileName) ? fallbackName : fileName;
        }
		
        //경로를 / 단위로 나누고 상위 폴더부터 차례로 생성
        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
			
            string normalizedPath = folder.Replace('\\', '/');
            string[] parts = normalizedPath.Split('/');
            string currentPath = parts[0];
			
            for (int i = 1; i < parts.Length; i++)
            {
                string nextPath = currentPath + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(nextPath))
                    AssetDatabase.CreateFolder(currentPath, parts[i]);
				
                currentPath = nextPath;
            }
        }
		
    }
}
#endif
