using System;
using UnityEngine;

public class PitchGameController : MonoBehaviour
{
    private readonly string[] expectedNotes = { "Do", "Re", "Mi", "So" };
    private int currentIndex = 0;

    [Header("音效設定")]
    public AudioSource audioSource;
    public AudioClip doClip;
    public AudioClip reClip;
    public AudioClip miClip;
    public AudioClip soClip;
    public AudioClip finishClip;

    void OnEnable()
    {
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
        SolfegeMessage msg;
        try
        {
            msg = JsonUtility.FromJson<SolfegeMessage>(json);
        }
        catch
        {
            Debug.LogWarning("非 Solfege 訊息，略過：" + json);
            return;
        }

        if (msg == null || string.IsNullOrEmpty(msg.solfege)) return;

        string expected = expectedNotes[currentIndex];
        if (msg.solfege == expected)
        {
            Debug.Log($"唱對了：{msg.solfege}");
            PlayNoteAudio(msg.solfege);
            currentIndex++;

            if (currentIndex >= expectedNotes.Length)
            {
                audioSource.PlayOneShot(finishClip);
                Debug.Log("完成全部音階！");
            }
        }
        else
        {
            Debug.Log($"收到音階 {msg.solfege}，但當前期待 {expected}，略過");
        }
    }

    void PlayNoteAudio(string note)
    {
        switch (note)
        {
            case "Do": audioSource.PlayOneShot(doClip); break;
            case "Re": audioSource.PlayOneShot(reClip); break;
            case "Mi": audioSource.PlayOneShot(miClip); break;
            case "So": audioSource.PlayOneShot(soClip); break;
        }
    }

    [Serializable]
    public class SolfegeMessage
    {
        public string solfege;
    }
}
