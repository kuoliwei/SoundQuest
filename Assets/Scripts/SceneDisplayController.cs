using UnityEngine;
using UnityEngine.Video;

public class SceneDisplayController : MonoBehaviour
{
    [SerializeField] private GameObject pitchDisplayCanvas;
    [SerializeField] private GameObject volumeDisplayCanvas;
    private PitchGameController pitchGameController;
    private VolumeGameController volumeGameController;
    void Start()
    {
        pitchGameController = pitchDisplayCanvas.GetComponent<PitchGameController>();
        volumeGameController = volumeDisplayCanvas.GetComponent<VolumeGameController>();
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
        if (!pitchDisplayCanvas.activeSelf)
        {
            if (WebSocketClient.Instance != null)
                WebSocketClient.Instance.SendJson(new Mode { mode = "solfege" });
            pitchDisplayCanvas.SetActive(true);
            volumeDisplayCanvas.SetActive(false);
        }
        else
        {
            pitchGameController.HardReset();
        }
    }

    public void SwitchToVolume()
    {
        if (!volumeDisplayCanvas.activeSelf)
        {
            if (WebSocketClient.Instance != null)
                WebSocketClient.Instance.SendJson(new Mode { mode = "db" });

            pitchDisplayCanvas.SetActive(false);
            volumeDisplayCanvas.SetActive(true);
        }
        else
        {
            volumeGameController.HardReset();
        }
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (pitchDisplayCanvas.activeSelf)
            {
                pitchGameController.TriggerDo();
            }
            else if (volumeDisplayCanvas.activeSelf)
            {
                volumeGameController.Trigger70();
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (pitchDisplayCanvas.activeSelf)
            {
                pitchGameController.TriggerMi();
            }
            else if (volumeDisplayCanvas.activeSelf)
            {
                volumeGameController.Trigger90();
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            if (pitchDisplayCanvas.activeSelf)
            {
                pitchGameController.TriggerFa();
            }
            else if (volumeDisplayCanvas.activeSelf)
            {
                volumeGameController.Trigger110();
            }
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (pitchDisplayCanvas.activeSelf)
            {
                pitchGameController.TriggerSo();
            }
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (pitchDisplayCanvas.activeSelf)
            {
                pitchGameController.ToggleTestMode();
            }
            else if (volumeDisplayCanvas.activeSelf)
            {
                volumeGameController.ToggleTestMode();
            }
        }
    }
}
