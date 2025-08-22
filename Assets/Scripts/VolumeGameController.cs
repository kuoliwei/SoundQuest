using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using static ParticleController;

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
    [SerializeField] Text dbValue;
    private int currentIndex = 0;
    private bool isLocked = false;
    [SerializeField] float showVideoDelay;

    // VolumeGameController.cs 片段：新增欄位
    [SerializeField] ParticleController particleController;

    // 目標區間（正規化模式使用）
    [SerializeField] Vector2 sizeRange;  // 最細~最粗
    [SerializeField] Vector2 speedRange;   // 最慢~最快

    // --- 粗細/速度映射設定 ---
    [Header("dB → 粗細/速度映射")]
    [SerializeField] float minDb;                  // 正規化下限
    [SerializeField] float maxDb;                 // 正規化上限

    [Header("關卡通過條件")]
    [SerializeField] private int requiredSuccessCount;  // 需要連續幾次正確才算過關
    private int successCount = 0;                           // 當前連續正確次數

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
    void DisplayDbValue(float dB, Color color)
    {
        dbValue.color = color;
        dbValue.text = ((int)dB).ToString();
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
        DisplayDbValue(dBAfterWeight, particleController.CurrentColor);
        float t = Mathf.Clamp01(Mathf.InverseLerp(minDb, maxDb, dBAfterWeight));
        float size = Mathf.Lerp(sizeRange.x, sizeRange.y, t);
        float speed = Mathf.Lerp(speedRange.x, speedRange.y, t);
        particleController.SetTrailWidthAbs(size);
        particleController.SetTrailSpeedAbs(speed);

        if (Mathf.Abs(dBAfterWeight - expected) <= toleranceSlider.value)
        {
            successCount++;
            if (successCount >= requiredSuccessCount)
            {
                Debug.Log($"收到dB：{dB}，經過加權{weightSlider.value}倍為{dBAfterWeight} dB，當前關卡{expected} dB，誤差未超出容錯{toleranceSlider.value} dB，連續 {successCount} 次正確，通過");
                console.text = $"收到dB：{dB}，經過加權{weightSlider.value}倍為{dBAfterWeight} dB，當前關卡{expected} dB，誤差未超出容錯{toleranceSlider.value} dB，連續 {successCount} 次正確，通過";
                PlayStageVideo(currentIndex);
                currentIndex++;
                successCount = 0; // 重置下一關重新計算
            }
            else
            {
                Debug.Log($"收到dB：{dB}，經過加權{weightSlider.value}倍為{dBAfterWeight} dB，當前關卡{expected} dB，誤差未超出容錯{toleranceSlider.value} dB，連續 {successCount} 次正確");
                console.text = $"收到dB：{dB}，經過加權{weightSlider.value}倍為{dBAfterWeight} dB，當前關卡{expected} dB，誤差未超出容錯{toleranceSlider.value} dB，連續 {successCount} 次正確";
            }
        }
        else
        {
            Debug.Log($"收到 dB={dB}，經過加權{weightSlider.value}倍為{dBAfterWeight} dB，但目前預期為 {expected} dB，誤差超出容錯{toleranceSlider.value} dB，忽略");
            console.text = $"收到 dB={dB}，經過加權{weightSlider.value}倍為{dBAfterWeight} dB，但目前預期為 {expected} dB，誤差超出容錯{toleranceSlider.value} dB，忽略";
            successCount = 0;
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

            //// 如果是最後階段，等播放完才播放完成影片
            //if (index == expectedVolumes.Length - 1)
            //{
            //    Invoke(nameof(PlayFinalVideo), (float)clip.length + 0.1f);
            //}

            // 如果不播最後影片
            if (index == expectedVolumes.Length - 1)
            {
                Invoke(nameof(ResetStage), (float)clip.length + 0.1f);
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
        if(currentIndex <= 2)
        {
            particleController.ResumeEmission();
            dbValue.gameObject.SetActive(true);
            particleController.SetEmissionColor((ParticleController.EmissionColor)currentIndex);
        }
        Debug.Log("影片播放結束，畫面變黑");
    }
    void ShowVideo()
    {
        videoImage.color = Color.white;
        particleController.PauseEmission();
        dbValue.gameObject.SetActive(false);
        Debug.Log("影片播放開始，畫面變白");
    }
    void ResetStage()
    {
        currentIndex = 0;
        successCount = 0;   // 也要歸零
        isLocked = false;
        videoImage.color = Color.black;
        particleController.ResumeEmission();
        dbValue.gameObject.SetActive(true);
        particleController.SetEmissionColor((ParticleController.EmissionColor)currentIndex);
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
