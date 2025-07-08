using UnityEngine;
using UnityEngine.Video;

public class SceneDisplayController : MonoBehaviour
{
    [SerializeField] private GameObject pitchDisplayCanvas;
    [SerializeField] private GameObject volumeDisplayCanvas;

    void Start()
    {
        // 預設副銀幕顯示空白（黑畫面）
        pitchDisplayCanvas.SetActive(false);
        volumeDisplayCanvas.SetActive(false);

        // 啟動第二螢幕（Display 1）
        if (Display.displays.Length > 1)
        {
            Display.displays[1].Activate();
        }
    }

    public void SwitchToPitch()
    {
        WebSocketClient.Instance.SendJson(new ModeMessage { mode = "6" });
        pitchDisplayCanvas.SetActive(true);
        volumeDisplayCanvas.SetActive(false);
    }

    public void SwitchToVolume()
    {
        WebSocketClient.Instance.SendJson(new ModeMessage { mode = "16" });
        pitchDisplayCanvas.SetActive(false);
        volumeDisplayCanvas.SetActive(true);
    }

    [System.Serializable]
    private class ModeMessage
    {
        public string mode;
    }
}
