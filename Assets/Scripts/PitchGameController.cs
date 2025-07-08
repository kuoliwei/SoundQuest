using System;
using UnityEngine;

public class PitchGameController : MonoBehaviour
{
    private readonly string[] expectedNotes = { "Do", "Re", "Mi", "So" };
    private int currentIndex = 0;

    [Header("���ĳ]�w")]
    public AudioSource audioSource;
    public AudioClip doClip;
    public AudioClip reClip;
    public AudioClip miClip;
    public AudioClip soClip;
    public AudioClip finishClip;

    void OnEnable()
    {
        WebSocketClient.Instance.OnMessageReceived += HandleMessage;
        currentIndex = 0; // �C���ҥγ����m�y�{
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
            Debug.LogWarning("�D Solfege �T���A���L�G" + json);
            return;
        }

        if (msg == null || string.IsNullOrEmpty(msg.solfege)) return;

        string expected = expectedNotes[currentIndex];
        if (msg.solfege == expected)
        {
            Debug.Log($"�۹�F�G{msg.solfege}");
            PlayNoteAudio(msg.solfege);
            currentIndex++;

            if (currentIndex >= expectedNotes.Length)
            {
                audioSource.PlayOneShot(finishClip);
                Debug.Log("�������������I");
            }
        }
        else
        {
            Debug.Log($"���쭵�� {msg.solfege}�A����e���� {expected}�A���L");
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
