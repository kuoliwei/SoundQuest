using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VolumeGameController : MonoBehaviour
{
    [Header("影片播放")]
    public RawImage videoImage;
    public VideoPlayer videoPlayer;
    public VideoClip clip50;
    public VideoClip clip70;
    public VideoClip clip90;
    public VideoClip clipFinal;

    private readonly int[] expectedVolumes = { 50, 70, 90 };
    [Range(0f, 10f)] [SerializeField] float tolerance;
    [SerializeField] Slider toleranceSlider;
    [SerializeField] Text toleranceValue;
    [Range(0f, 2f)][SerializeField] float weight;
    [SerializeField] Slider weightSlider;
    [SerializeField] Text weightValue;

    [SerializeField] Text console;
    private int currentIndex = 0;
    private bool isLocked = false;
    [SerializeField] float showVideoDelay;

    void OnEnable()
    {
        toleranceSlider.value = tolerance;
        weightSlider.value = weight;
        OnToleranceValueChange();
        if (WebSocketClient.Instance != null)
            WebSocketClient.Instance.OnMessageReceived += HandleMessage;

        ResetStage();
    }

    void OnDisable()
    {
        if (WebSocketClient.Instance != null)
            WebSocketClient.Instance.OnMessageReceived -= HandleMessage;
    }

    void HandleMessage(string json)
    {
        if (isLocked)
        {
            Debug.Log("正在播放影片中，期間不接受音量輸入");
            return;
        }

        Volume msg;
        try
        {
            msg = JsonUtility.FromJson<Volume>(json);
        }
        catch
        {
            return;
        }

        if (!int.TryParse(msg.dB, out int dB))
        {
            Debug.LogWarning("dB 欄位不是合法整數：" + msg.dB);
            return;
        }

        if (currentIndex >= expectedVolumes.Length)
        {
            Debug.Log("所有階段已完成，等待完成影片播放");
            return;
        }

        int expected = expectedVolumes[currentIndex];

        float dBAfterWeight = dB * weightSlider.value;
        if (Mathf.Abs(dBAfterWeight - expected) <= toleranceSlider.value)
        {
            Debug.Log($"收到dB：{dB}，經過加權{weightSlider.value}倍為{dBAfterWeight} dB，當前關卡{expected} dB，誤差未超出容錯{toleranceSlider.value} dB");
            console.text = $"收到dB：{dB}，經過加權{weightSlider.value}倍為{dBAfterWeight} dB，當前關卡{expected} dB，誤差未超出容錯{toleranceSlider.value} dB";
            PlayStageVideo(currentIndex);
            currentIndex++;
        }
        else
        {
            Debug.Log($"收到 dB={dB}，經過加權{weightSlider.value}倍為{dBAfterWeight} dB，但目前預期為 {expected} dB，誤差超出容錯{toleranceSlider.value} dB，忽略");
            console.text = $"收到 dB={dB}，經過加權{weightSlider.value}倍為{dBAfterWeight} dB，但目前預期為 {expected} dB，誤差超出容錯{toleranceSlider.value} dB，忽略";
        }
    }

    void PlayStageVideo(int index)
    {
        isLocked = true;

        VideoClip clip = index switch
        {
            0 => clip50,
            1 => clip70,
            2 => clip90,
            _ => null
        };

        if (clip != null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = clip;
            videoPlayer.Play();
            Debug.Log($"播放階段 {index} 影片：{clip.name}");
            Invoke(nameof(ShowVideo), showVideoDelay);
            Invoke(nameof(HideVideo), (float)clip.length);
            Invoke(nameof(UnlockInput), (float)clip.length);

            // 如果是最後階段，等播放完才播放完成影片
            if (index == expectedVolumes.Length - 1)
            {
                Invoke(nameof(PlayFinalVideo), (float)clip.length + 0.1f);
            }
        }
    }

    void PlayFinalVideo()
    {
        isLocked = true;
        videoPlayer.Stop();
        videoPlayer.clip = clipFinal;
        videoPlayer.Play();
        Debug.Log("三階段完成，播放最終影片！");
        Invoke(nameof(ShowVideo), showVideoDelay);
        Invoke(nameof(ResetStage), (float)clipFinal.length + 0.1f);
    }

    void UnlockInput()
    {
        isLocked = false;
        Debug.Log("解鎖輸入，允許下一階段");
    }
    void HideVideo()
    {
        videoImage.color = Color.black;
        Debug.Log("影片播放結束，畫面變黑");
    }
    void ShowVideo()
    {
        videoImage.color = Color.white;
        Debug.Log("影片播放開始，畫面變白");
    }
    void ResetStage()
    {
        currentIndex = 0;
        isLocked = false;
        videoImage.color = Color.black;
        Debug.Log("重置進度：currentIndex = 0，isLocked = false");
    }
    public void OnToleranceValueChange()
    {
        toleranceValue.text = toleranceSlider.value.ToString();
    }
    public void OnWeightValueChange()
    {
        weightValue.text = weightSlider.value.ToString();
    }
}
