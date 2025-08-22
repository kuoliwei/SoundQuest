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
#if !UNITY_EDITOR
        // 啟動第二螢幕（Display 1）
        if (Display.displays.Length > 1)
        {
            Display.displays[1].Activate();
        }
#endif
    }

    public void SwitchToPitch()
    {
        WebSocketClient.Instance.SendJson(new Mode { mode = "solfege" });
        pitchDisplayCanvas.SetActive(true);
        volumeDisplayCanvas.SetActive(false);
    }

    public void SwitchToVolume()
    {
        WebSocketClient.Instance.SendJson(new Mode { mode = "db" });
        pitchDisplayCanvas.SetActive(false);
        volumeDisplayCanvas.SetActive(true);
    }
}
