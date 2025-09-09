using System;
using UnityEngine;
using UnityEngine.UI;

public class PitchGameController : MonoBehaviour
{
    private readonly string[] expectedNotes = { "Do", "Mi", "Fa", "Sol" };
    private int currentIndex = 0;
    private bool isLocked = false;
    private bool isStageClosed = false;

    [SerializeField] Text console;

    [Header("���ĳ]�w")]
    public AudioSource audioSource;
    public AudioClip doClip;
    public AudioClip miClip;
    public AudioClip faClip;
    public AudioClip soClip;
    public AudioClip finishClip;

    [Header("���ռҦ�")]
    [SerializeField] private bool isTestMode = false;
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
    public void ToggleTestMode()
    {
        if (gameObject.activeSelf)
        {
            if (isTestMode)
            {
                isTestMode = false;
                console.text = "�������ռҦ��A�i���i���d";
            }
            else
            {
                isTestMode = true;
                console.text = "�Ұʴ��ռҦ��A�Ȱ����i���d";
            }
        }
    }
    public void HardReset()
    {
        //if (WebSocketClient.Instance != null)
        //    WebSocketClient.Instance.OnMessageReceived -= HandleMessage;
        //if (WebSocketClient.Instance != null)
        //    WebSocketClient.Instance.OnMessageReceived += HandleMessage;
        //currentIndex = 0; // �C���ҥγ����m�y�{
        //ResetStage();
        // 1) �M�� Invoke�B������
        CancelInvoke();
        if (audioSource != null) audioSource.Stop();

        // 2) �����ƥ�]���h�A���^
        if (WebSocketClient.Instance != null)
            WebSocketClient.Instance.OnMessageReceived -= HandleMessage;
        if (WebSocketClient.Instance != null)
            WebSocketClient.Instance.OnMessageReceived += HandleMessage;

        // 3) �_�쪬�A
        currentIndex = 0;
        ResetStage();
    }
    void HandleMessage(string json)
    {
        if (isLocked && !isStageClosed)
        {
            Debug.Log("���b����L�����Ĥ��A�������������O");
            console.text = "���b����L�����Ĥ��A�������������O";
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

        if (msg.solfege == expected && !isTestMode)
        {
            console.text = $"���� {msg.solfege}�A��e���d���ӬO {expected}�A�q�L";
            PlayNoteAudio(msg.solfege);
            currentIndex++;
        }
        else if(!isTestMode)
        {
            Debug.Log($"���� {msg.solfege}�A����e���d���ӬO {expected}");
            console.text = $"���� {msg.solfege}�A����e���d���ӬO {expected}";
        }
        if (isTestMode && gameObject.activeSelf)
        {
            Debug.Log($"����ռҦ����� {msg.solfege}");
            console.text = $"����ռҦ����� {msg.solfege}";
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
        Debug.Log("�����J�A���\�i�J�U�@��");
        console.text = "�����J�A���\�i�J�U�@��";
        isLocked = false;
    }
    void PlayFinishClip()
    {
        Debug.Log("�Ҧ��������T�����A���񧹦�����");
        console.text = "�Ҧ��������T�����A���񧹦�����";
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
        isStageClosed = true;

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
        currentIndex = 0;
        isLocked = false;
        isStageClosed = false;
        isTestMode = false;
        Debug.Log("���m���d�GcurrentIndex = 0�AisLocked = false");
        console.text = "�}�l�ۦW����";
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
