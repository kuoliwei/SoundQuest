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
        // 等完成音效播完後再重設關卡
        //Invoke(nameof(ResetStage), finishClip.length);
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
