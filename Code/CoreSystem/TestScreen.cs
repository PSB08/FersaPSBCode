using UnityEngine;

namespace Work.PSB.Code.CoreSystem
{
    public class TestScreen : MonoBehaviour
    {
        void Start()
        {
            Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);
        }
        
    }
}