using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Work.PSB.Code.CoreSystem.TutoSystem
{
    [CreateAssetMenu(fileName = "TutoPage", menuName = "SO/Tutorial/Page", order = 0)]
    public class TutorialPageSO : ScriptableObject
    {
        public string title;
        public VideoClip videoClip;
        public Sprite image;
        [TextArea(3, 6)]
        public string description;
    }
}