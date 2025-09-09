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
    public VideoClip clip70;
    public VideoClip clip90;
    public VideoClip clip110;
    public VideoClip clipFinal;

    private readonly int[] expectedVolumes = { 70, 90, 110 };
    [Range(0, 10)] [SerializeField] int tolerance;
    [SerializeField] Slider toleranceSlider;
    [SerializeField] Text toleranceValue;
    [Range(0, 20)][SerializeField] int weight;
    [SerializeField] Slider weightSlider;
    [SerializeField] Text weightValue;
    private float WeightSliderValue => weightSlider.value / 10f;

    [SerializeField] Text console;
    [SerializeField] Text dbValue;
    private int currentIndex = 0;
    private bool isLocked = false;
    private bool isStageClosed = false;
    [SerializeField] float showVideoDelay;

    //// VolumeGameController.cs 片段：新增欄位
    //[SerializeField] ParticleController particleController;

    // VolumeGameController.cs 片段：新增欄位
    [SerializeField] SoundEffectManager soundEffectManager;

    // 目標區間（正規化模式使用）
    [SerializeField] Vector2 sizeRange;  // 最細~最粗
    [SerializeField] Vector2 speedRange;   // 最慢~最快

    // --- 粗細/速度映射設定 ---
    [Header("dB → 粗細/速度映射")]
    [SerializeField] float minDb;                  // 正規化下限
    [SerializeField] float maxDb;                 // 正規化上限

    [Header("關卡通過條件")]
    [Range(1, 11)] [SerializeField] private int requiredSuccessCount;  // 需要連續幾次正確才算過關
    private int successCount = 1;                           // 當前連續正確次數
    [SerializeField] Slider requiredSuccessSlider;
    private int RequiredSuccessCount => (int)requiredSuccessSlider.value == 1 ? 1 : ((int)requiredSuccessSlider.value - 1) * 10;
    [SerializeField] Text requiredSuccessValue;

    [Header("流程設定")]
    [SerializeField] private bool restartAfterFinal = false;
    // 勾選：最終影片播畢後重頭開始；不勾：播畢後關閉關卡（全黑無特效）

    [Header("測試模式")]
    [SerializeField] private bool isTestMode = false;

    void OnEnable()
    {
        if (WebSocketClient.Instance != null)
            WebSocketClient.Instance.OnMessageReceived += HandleMessage;
        ResetStage();
    }

    void OnDisable()
    {
        if (WebSocketClient.Instance != null)
            WebSocketClient.Instance.OnMessageReceived -= HandleMessage;
    }
    public void ToggleTestMode()
    {
        if (gameObject.activeSelf)
        {
            if (isTestMode)
            {
                isTestMode = false;
                console.text = "取消測試模式，可推進關卡";
            }
            else
            {
                isTestMode = true;
                console.text = "啟動測試模式，暫停推進關卡";
            }
        }
    }
    public void HardReset()
    {
        //if (WebSocketClient.Instance != null)
        //    WebSocketClient.Instance.OnMessageReceived -= HandleMessage;
        //toleranceSlider.value = tolerance;
        //weightSlider.value = weight;
        //OnToleranceValueChange();
        //if (WebSocketClient.Instance != null)
        //    WebSocketClient.Instance.OnMessageReceived += HandleMessage;
        //ResetStage();
        // 1) 先清除既有 Invoke 與播放狀態
        CancelInvoke();
        if (videoPlayer != null) { videoPlayer.Stop(); videoPlayer.clip = null; }
        if (videoImage != null) { videoImage.color = Color.black; }

        // 2) 重掛事件（避免重複訂閱）
        if (WebSocketClient.Instance != null)
            WebSocketClient.Instance.OnMessageReceived -= HandleMessage;

        // 同步滑桿文字
        if (toleranceSlider != null) toleranceSlider.value = tolerance;
        if (weightSlider != null) weightSlider.value = weight;

        if (WebSocketClient.Instance != null)
            WebSocketClient.Instance.OnMessageReceived += HandleMessage;

        // 3) 復位狀態 + UI/特效
        ResetStage();
    }
    void DisplayDbValue(float dB, Color color)
    {
        dbValue.color = color;
        dbValue.text = ((int)dB).ToString();
    }
    void HandleMessage(string json)
    {
        if (isLocked && !isStageClosed)
        {
            Debug.Log("正在播放影片中，期間不接受音量輸入");
            console.text = "正在播放影片中，期間不接受音量輸入";
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

        int dBAfterWeight = Mathf.RoundToInt(dB * WeightSliderValue);
        DisplayDbValue(dBAfterWeight, Color.white);
        //float t = Mathf.Clamp01(Mathf.InverseLerp(minDb, maxDb, dBAfterWeight));
        //float size = Mathf.Lerp(sizeRange.x, sizeRange.y, t);
        //float speed = Mathf.Lerp(speedRange.x, speedRange.y, t);
        soundEffectManager.SetAmplitude(dBAfterWeight);
        //particleController.SetTrailWidthAbs(size);
        //particleController.SetTrailSpeedAbs(speed);

        if (Mathf.Abs(dBAfterWeight - expected) <= toleranceSlider.value && !isTestMode)
        {
            successCount++;
            if (successCount >= RequiredSuccessCount)
            {
                Debug.Log($"收到dB：{dB}，經過加權{WeightSliderValue}倍為{dBAfterWeight} dB，當前關卡{expected} dB，誤差未超出容錯{toleranceSlider.value} dB，連續 {successCount} 次正確，通過");
                console.text = $"收到dB：{dB}，經過加權{WeightSliderValue}倍為{dBAfterWeight} dB，當前關卡{expected} dB，誤差未超出容錯{toleranceSlider.value} dB，連續 {successCount} 次正確，通過";
                PlayStageVideo(currentIndex);
                currentIndex++;
                successCount = 0; // 重置下一關重新計算
            }
            else
            {
                Debug.Log($"收到dB：{dB}，經過加權{WeightSliderValue}倍為{dBAfterWeight} dB，當前關卡{expected} dB，誤差未超出容錯{toleranceSlider.value} dB，累計 {successCount} 次正確");
                console.text = $"收到dB：{dB}，經過加權{WeightSliderValue}倍為{dBAfterWeight} dB，當前關卡{expected} dB，誤差未超出容錯{toleranceSlider.value} dB，累計 {successCount} 次正確";
            }
        }
        else if (!isTestMode)
        {
            Debug.Log($"收到 dB={dB}，經過加權{WeightSliderValue}倍為{dBAfterWeight} dB，但目前預期為 {expected} dB，誤差超出容錯{toleranceSlider.value} dB，忽略");
            console.text = $"收到 dB={dB}，經過加權{WeightSliderValue}倍為{dBAfterWeight} dB，但目前預期為 {expected} dB，誤差超出容錯{toleranceSlider.value} dB，忽略";
            //successCount = 0; // 若要改成累計制而非連續制就註解掉這行
        }
        if (isTestMode && gameObject.activeSelf)
        {
            Debug.Log($"於測試模式收到 dB={dB}，經過加權{WeightSliderValue}倍為{dBAfterWeight} dB");
            console.text = $"於測試模式收到 dB={dB}，經過加權{WeightSliderValue}倍為{dBAfterWeight} dB";
        }
    }

    void PlayStageVideo(int index)
    {
        isLocked = true;

        VideoClip clip = index switch
        {
            0 => clip70,
            1 => clip90,
            2 => clip110,
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


            if (clipFinal != null)
            {
                // 如果要撥放最後影片，最後階段，等播放完最後關卡影片才播放最後影片
                if (index == expectedVolumes.Length - 1)
                {
                    Invoke(nameof(PlayFinalVideo), (float)clip.length + 0.1f);
                }
            }
            else
            {
                // 如果不播最後影片
                if (index == expectedVolumes.Length - 1)
                {
                    //Invoke(nameof(ResetStage), (float)clip.length + 0.1f);  //  結束後自動重啟遊戲
                    Invoke(nameof(CloseStage), (float)clip.length + 0.1f);  //  結束後不自動重啟遊戲
                }
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
        //Invoke(nameof(ResetStage), (float)clipFinal.length + 0.1f);

        // 影片播完之後：依設定決定 Reset 或 Close
        float endAt = (float)clipFinal.length + 0.05f;
        if (restartAfterFinal)
        {
            // 重頭開始：回到初始狀態（會恢復粒子、顯示 dB 等）
            Invoke(nameof(ResetStage), endAt);
        }
        else
        {
            // 關閉關卡：全黑無特效 & 鎖住輸入
            Invoke(nameof(CloseStage), endAt);
        }
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
            soundEffectManager.ResumeEmission();
            dbValue.gameObject.SetActive(true);
            //particleController.SetEmissionColor((ParticleController.EmissionColor)currentIndex);
        }
        Debug.Log("影片播放結束，畫面變黑");
    }
    void ShowVideo()
    {
        videoImage.color = Color.white;
        soundEffectManager.PauseEmission();
        dbValue.gameObject.SetActive(false);
        Debug.Log("影片播放開始，畫面變白");
    }
    /// <summary>
    /// 關閉關卡：畫面全黑、停止影片畫面、關閉粒子/特效、隱藏數值顯示，並鎖住輸入。
    /// </summary>
    public void CloseStage()
    {
        // 停掉未來可能的排程（避免殘留 Invoke 導致又被喚起 Show/Hide）
        CancelInvoke();

        // 鎖住輸入，避免後續又吃到訊息
        isLocked = true;
        isStageClosed = true;

        // 視覺：全黑、停止影片
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = null; // 可選：清空 clip，保證完全靜默
        }
        if (videoImage != null)
        {
            videoImage.color = Color.black;
        }

        // 特效：完全關閉（這裡用 Pause 代表停止發射 & 不顯示）
        if (soundEffectManager != null)
        {
            soundEffectManager.PauseEmission();
        }

        // UI 顯示：隱藏 dB
        if (dbValue != null)
        {
            dbValue.gameObject.SetActive(false);
        }

        // 記錄狀態
        Debug.Log("[CloseStage] 已關閉關卡：畫面全黑、無特效、輸入鎖定");
        if (console != null)
        {
            console.text = "關卡已結束（畫面全黑，無特效）";
        }
    }

    void ResetStage()
    {
        currentIndex = 0;
        successCount = 0;   // 也要歸零
        isLocked = false;
        isStageClosed = false;
        isTestMode = false;
        videoImage.color = Color.black;
        soundEffectManager.ResumeEmission();
        dbValue.gameObject.SetActive(true);
        toleranceSlider.value = tolerance;
        weightSlider.value = weight;
        requiredSuccessSlider.value = requiredSuccessCount;
        OnToleranceValueChange();
        OnWeightValueChange();
        OnRequiredSuccessValueChange();
        //particleController.SetEmissionColor((ParticleController.EmissionColor)currentIndex);
        Debug.Log("重置進度：currentIndex = 0，isLocked = false");
        console.text = "開始分貝辨識";
    }
    public void OnToleranceValueChange()
    {
        toleranceValue.text = toleranceSlider.value.ToString();
    }
    public void OnWeightValueChange()
    {
        weightValue.text = WeightSliderValue.ToString();
    }
    public void OnRequiredSuccessValueChange()
    {
        requiredSuccessValue.text = RequiredSuccessCount.ToString();
    }

    // --- 直接觸發三個音量關卡 ---
    public void Trigger70()
    {
        SimulatePassForTarget(70);
    }

    public void Trigger90()
    {
        SimulatePassForTarget(90);
    }

    public void Trigger110()
    {
        SimulatePassForTarget(110);
    }

    /// <summary>
    /// 依目標 dB 值建立一筆 JSON 丟給 HandleMessage，保持與 WebSocket 相同流程。
    /// </summary>
    private void SimulatePassForTarget(int targetDb)
    {
        //if (isLocked) return;

        //// 避免除以零，反推一個原始 dB，讓加權後剛好是目標值
        //float w = Mathf.Max(0.1f, weightSlider != null ? WeightSliderValue : 1f);
        //int injectedDb = Mathf.RoundToInt((float)targetDb / w);
        //Debug.Log($"{WeightSliderValue},{w},{injectedDb}");
        //// 組 JSON
        //string json = "{\"dB\":\"" + injectedDb.ToString() + "\"}";

        //// 依 requiredSuccessCount 連續送入多次，保證直接過關
        //int times = Mathf.Max(1, RequiredSuccessCount);
        //for (int i = 0; i < times; i++)
        //{
        //    HandleMessage(json);
        //    if (isLocked) break; // 一旦過關就會被鎖住，之後不用再送
        //}

        if (isLocked || isStageClosed) return;
        if (currentIndex >= expectedVolumes.Length) return;

        int expected = expectedVolumes[currentIndex];
        if (expected != targetDb)
        {
            if (console != null) console.text = $"[TEST] 目前關卡是 {expected} dB，忽略 {targetDb} dB";
            return;
        }
        soundEffectManager.SetAmplitude(targetDb);
        //// 對齊目前要通過的關卡索引（若你想強制跳關就保留；若只允許通過當前關卡，可改用 currentIndex 檢查）
        //int targetIndex = System.Array.IndexOf(expectedVolumes, targetDb);
        //if (targetIndex < 0) return;

        //// 若想嚴格只能通過「目前所在的關卡」，就取消下一行並改用 if (targetIndex != currentIndex) return;
        //currentIndex = targetIndex;

        // 直接視為已達成通過條件
        successCount = RequiredSuccessCount;

        // 播該關卡影片與調度後續流程（Show/Hide/Unlock/Final/Close 都在這裡處理）
        PlayStageVideo(currentIndex);

        // 推進到下一關，並把連續計數歸零，與 HandleMessage 成功分支保持一致
        currentIndex++;
        successCount = 0;

        // UI 提示（可選）
        if (console != null)
            console.text = $"[TEST] 直接通過 {targetDb} dB 關卡";
    }
}
