using System;
using UnityEngine;
using UnityEngine.UI;

public class PitchGameController : MonoBehaviour
{
    private readonly string[] expectedNotes = { "Do", "Mi", "Fa", "Sol" };
    private int currentIndex = 0;
    private bool isLocked = false;

    [SerializeField] Text console;

    [Header("音效設定")]
    public AudioSource audioSource;
    public AudioClip doClip;
    public AudioClip miClip;
    public AudioClip faClip;
    public AudioClip soClip;
    public AudioClip finishClip;

    void OnEnable()
    {
        if (WebSocketClient.Instance != null)
            WebSocketClient.Instance.OnMessageReceived += HandleMessage;
        currentIndex = 0; // 每次啟用都重置流程
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
            Debug.Log($"正在播放音效中，期間不接受指令");
            return;
        }

        Pitch msg;
        try
        {
            msg = JsonUtility.FromJson<Pitch>(json);
        }
        catch
        {
            return;
        }

        if (string.IsNullOrEmpty(msg.solfege)) return;

        string expected = "";

        if (currentIndex >= expectedNotes.Length)
        {
            Debug.Log("等待完成音效結束中，忽略輸入");
            return;
        }
        else
        {
            expected = expectedNotes[currentIndex];
        }

        if (msg.solfege == expected)
        {
            console.text = $"收到 {msg.solfege}，當前關卡應該是 {expected}，通過";
            PlayNoteAudio(msg.solfege);
            currentIndex++;
        }
        else
        {
            Debug.Log($"收到 {msg.solfege}，但當前關卡應該是 {expected}");
            console.text = $"收到 {msg.solfege}，但當前關卡應該是 {expected}";
        }
    }

    void PlayNoteAudio(string note)
    {
        isLocked = true;

        switch (note)
        {
            case "Do":
                Debug.Log("播放音效：Do");
                audioSource.PlayOneShot(doClip);
                Invoke(nameof(UnlockInput), doClip.length);
                break;
            case "Mi":
                Debug.Log("播放音效：Mi");
                audioSource.PlayOneShot(miClip);
                Invoke(nameof(UnlockInput), miClip.length);
                break;
            case "Fa":
                Debug.Log("播放音效：Fa");
                audioSource.PlayOneShot(faClip);
                Invoke(nameof(UnlockInput), faClip.length);
                break;
            case "Sol":
                Debug.Log("播放音效：Sol");
                audioSource.PlayOneShot(soClip);
                Invoke(nameof(UnlockInput), soClip.length);

                // 延遲播放完成音效
                Invoke(nameof(PlayFinishClip), soClip.length + 0.1f);
                break;
        }
    }
    void UnlockInput()
    {
        Debug.Log("解鎖輸入，允許下一關");
        isLocked = false;
    }
    void PlayFinishClip()
    {
        Debug.Log("所有音階正確完成，播放完成音效");
        audioSource.PlayOneShot(finishClip);

        // 等完成音效播完後再呼叫 CloseStage
        if (finishClip != null)
        {
            Invoke(nameof(CloseStage), finishClip.length);
        }
        else
        {
            // 如果沒設定 clip，就立刻結束關卡
            CloseStage();
        }
        // 等完成音效播完後再重設關卡
        //Invoke(nameof(ResetStage), finishClip.length);
    }
    /// <summary>
    /// 關閉關卡：停止所有音效、鎖住輸入、顯示結束訊息。
    /// </summary>
    public void CloseStage()
    {
        // 1) 停掉未來可能的排程（避免已排程的 Unlock/FinishClip 又被叫起）
        CancelInvoke();

        // 2) 鎖住輸入，讓 HandleMessage 之後直接忽略來自 WebSocket 的事件
        isLocked = true;

        // 3) 停止目前可能正在播放的音效
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        // 4) UI/狀態：顯示結束訊息（可選）
        if (console != null)
        {
            console.text = "關卡已結束（已停止音效並鎖住輸入）";
        }

        // 5)（可選）把索引設到終點，避免外部誤用 currentIndex 狀態
        // currentIndex = expectedNotes.Length;

        Debug.Log("[Pitch] CloseStage：已關閉關卡、停止音效、輸入鎖定");
    }
    void ResetStage()
    {
        Debug.Log("重置關卡：currentIndex = 0，isLocked = false");
        currentIndex = 0;
        isLocked = false;
    }
    public void TriggerDo()
    {
        string json = JsonUtility.ToJson(new Pitch { solfege = "Do" });
        HandleMessage(json);
    }
    public void TriggerMi()
    {
        string json = JsonUtility.ToJson(new Pitch { solfege = "Mi" });
        HandleMessage(json);
    }
    public void TriggerFa()
    {
        string json = JsonUtility.ToJson(new Pitch { solfege = "Fa" });
        HandleMessage(json);
    }
    public void TriggerSo()
    {
        string json = JsonUtility.ToJson(new Pitch { solfege = "Sol" });
        HandleMessage(json);
    }
}
