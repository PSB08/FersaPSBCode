using System;
using System.Collections.Generic;
using UnityEngine;

namespace Work.PSB.Code.CoreSystem.TutoSystem
{
    public class TutorialArchiveListView : MonoBehaviour
    {
        [SerializeField] private Transform listRoot;
        [SerializeField] private TutorialArchiveListItemUI listItemPrefab;

        private readonly List<TutorialArchiveListItemUI> _itemUis = new();

        public event Action<int> OnItemClicked;

        public void Bind(List<TutorialDataSO> tutorials, int selectedIndex)
        {
            int count = tutorials != null ? tutorials.Count : 0;

            SyncItemCount(count);

            for (int i = 0; i < _itemUis.Count; i++)
            {
                bool active = i < count;
                _itemUis[i].gameObject.SetActive(active);

                if (!active)
                    continue;

                int capturedIndex = i;
                if (tutorials != null) 
                    _itemUis[i].Bind(tutorials[i], _ => OnItemClicked?.Invoke(capturedIndex));
                _itemUis[i].SetSelected(i == selectedIndex);
            }
        }

        public void SetSelected(int selectedIndex)
        {
            for (int i = 0; i < _itemUis.Count; i++)
            {
                if (_itemUis[i] == null || !_itemUis[i].gameObject.activeSelf)
                    continue;

                _itemUis[i].SetSelected(i == selectedIndex);
            }
        }

        public void SetInteractable(bool value)
        {
            for (int i = 0; i < _itemUis.Count; i++)
            {
                if (_itemUis[i] == null)
                    continue;

                _itemUis[i].SetInteractable(value);
            }
        }

        private void SyncItemCount(int count)
        {
            while (_itemUis.Count < count)
            {
                var item = Instantiate(listItemPrefab, listRoot);
                _itemUis.Add(item);
            }
        }
        
    }
}