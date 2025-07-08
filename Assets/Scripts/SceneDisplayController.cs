using UnityEngine;
using UnityEngine.Video;

public class SceneDisplayController : MonoBehaviour
{
    [SerializeField] private GameObject pitchDisplayCanvas;
    [SerializeField] private GameObject volumeDisplayCanvas;

    void Start()
    {
        // �w�]�ƻȹ���ܪťա]�µe���^
        pitchDisplayCanvas.SetActive(false);
        volumeDisplayCanvas.SetActive(false);

        // �ҰʲĤG�ù��]Display 1�^
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
