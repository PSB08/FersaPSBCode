using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Work.PSB.Code.FieldCode.MapSaves
{
    public class MapPlayingSaveSystem : MonoBehaviour
    {
        [SerializeField] private float autoSaveInterval = 60f;
        private float _timer;
        private bool _isDirty; // 데이터 변경 여부

        private void Start()
        {
            _isDirty = true;
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            if (_timer >= autoSaveInterval && _isDirty && CanSave())
            {
                Save();
            }
        }

        // 외부에서 호출
        public void MarkDirty()
        {
            _isDirty = true;
        }

        private bool CanSave()
        {
            // 나중에 조건 넣기
            return true;
        }
        
        public void DisableAutoSave()
        {
            _isDirty = false;
            this.enabled = false;
        }

        private void Save()
        {
            _timer = 0f;
            //_isDirty = false;

            SceneLoader.SaveCurrentScene(
                SceneManager.GetActiveScene().name
            );

            Debug.Log("AutoSave Complete");
        }

        /*private void OnApplicationQuit()
        {
            Save(); // 종료 시 강제 저장
        }*/
        
    }
}