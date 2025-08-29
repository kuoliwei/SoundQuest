using System;
using UnityEngine;
using UnityEngine.UI;

public class PitchGameController : MonoBehaviour
{
    private readonly string[] expectedNotes = { "Do", "Mi", "Fa", "Sol" };
    private int currentIndex = 0;
    private bool isLocked = false;

    [SerializeField] Text console;

    [Header("���ĳ]�w")]
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
        currentIndex = 0; // �C���ҥγ����m�y�{
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
            Debug.Log($"���b���񭵮Ĥ��A�������������O");
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
            Debug.Log("���ݧ������ĵ������A������J");
            return;
        }
        else
        {
            expected = expectedNotes[currentIndex];
        }

        if (msg.solfege == expected)
        {
            console.text = $"���� {msg.solfege}�A��e���d���ӬO {expected}�A�q�L";
            PlayNoteAudio(msg.solfege);
            currentIndex++;
        }
        else
        {
            Debug.Log($"���� {msg.solfege}�A����e���d���ӬO {expected}");
            console.text = $"���� {msg.solfege}�A����e���d���ӬO {expected}";
        }
    }

    void PlayNoteAudio(string note)
    {
        isLocked = true;

        switch (note)
        {
            case "Do":
                Debug.Log("���񭵮ġGDo");
                audioSource.PlayOneShot(doClip);
                Invoke(nameof(UnlockInput), doClip.length);
                break;
            case "Mi":
                Debug.Log("���񭵮ġGMi");
                audioSource.PlayOneShot(miClip);
                Invoke(nameof(UnlockInput), miClip.length);
                break;
            case "Fa":
                Debug.Log("���񭵮ġGFa");
                audioSource.PlayOneShot(faClip);
                Invoke(nameof(UnlockInput), faClip.length);
                break;
            case "Sol":
                Debug.Log("���񭵮ġGSol");
                audioSource.PlayOneShot(soClip);
                Invoke(nameof(UnlockInput), soClip.length);

                // ���𼽩񧹦�����
                Invoke(nameof(PlayFinishClip), soClip.length + 0.1f);
                break;
        }
    }
    void UnlockInput()
    {
        Debug.Log("�����J�A���\�U�@��");
        isLocked = false;
    }
    void PlayFinishClip()
    {
        Debug.Log("�Ҧ��������T�����A���񧹦�����");
        audioSource.PlayOneShot(finishClip);

        // ���������ļ�����A�I�s CloseStage
        if (finishClip != null)
        {
            Invoke(nameof(CloseStage), finishClip.length);
        }
        else
        {
            // �p�G�S�]�w clip�A�N�ߨ赲�����d
            CloseStage();
        }
        // ���������ļ�����A���]���d
        //Invoke(nameof(ResetStage), finishClip.length);
    }
    /// <summary>
    /// �������d�G����Ҧ����ġB����J�B��ܵ����T���C
    /// </summary>
    public void CloseStage()
    {
        // 1) �������ӥi�઺�Ƶ{�]�קK�w�Ƶ{�� Unlock/FinishClip �S�Q�s�_�^
        CancelInvoke();

        // 2) ����J�A�� HandleMessage ���᪽�������Ӧ� WebSocket ���ƥ�
        isLocked = true;

        // 3) ����ثe�i�ॿ�b���񪺭���
        if (audioSource != null)
        {
            audioSource.Stop();
        }

        // 4) UI/���A�G��ܵ����T���]�i��^
        if (console != null)
        {
            console.text = "���d�w�����]�w����Ĩ�����J�^";
        }

        // 5)�]�i��^����޳]����I�A�קK�~���~�� currentIndex ���A
        // currentIndex = expectedNotes.Length;

        Debug.Log("[Pitch] CloseStage�G�w�������d�B����ġB��J��w");
    }
    void ResetStage()
    {
        Debug.Log("���m���d�GcurrentIndex = 0�AisLocked = false");
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
