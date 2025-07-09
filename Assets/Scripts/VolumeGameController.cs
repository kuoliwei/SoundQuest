using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VolumeGameController : MonoBehaviour
{
    [Header("�v������")]
    public RawImage videoImage;
    public VideoPlayer videoPlayer;
    public VideoClip clip50;
    public VideoClip clip70;
    public VideoClip clip90;
    public VideoClip clipFinal;

    private readonly int[] expectedVolumes = { 50, 70, 90 };
    private int currentIndex = 0;
    private bool isLocked = false;

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

    void HandleMessage(string json)
    {
        if (isLocked)
        {
            Debug.Log("���b����v�����A�������������q��J");
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
            Debug.LogWarning("dB ��줣�O�X�k��ơG" + msg.dB);
            return;
        }

        if (currentIndex >= expectedVolumes.Length)
        {
            Debug.Log("�Ҧ����q�w�����A���ݧ����v������");
            return;
        }

        int expected = expectedVolumes[currentIndex];
        if (Mathf.Abs(dB - expected) <= 5)
        {
            PlayStageVideo(currentIndex);
            currentIndex++;
        }
        else
        {
            Debug.Log($"���� dB={dB}�A���ثe�w���� {expected}�A����");
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
            videoImage.color = Color.white;
            videoPlayer.Stop();
            videoPlayer.clip = clip;
            videoPlayer.Play();
            Debug.Log($"���񶥬q {index} �v���G{clip.name}");
            Invoke(nameof(HideVideo), (float)clip.length);
            Invoke(nameof(UnlockInput), (float)clip.length);

            // �p�G�O�̫ᶥ�q�A�����񧹤~���񧹦��v��
            if (index == expectedVolumes.Length - 1)
            {
                Invoke(nameof(PlayFinalVideo), (float)clip.length + 0.1f);
            }
        }
    }

    void PlayFinalVideo()
    {
        isLocked = true;
        videoImage.color = Color.white;
        videoPlayer.Stop();
        videoPlayer.clip = clipFinal;
        videoPlayer.Play();
        Debug.Log("�T���q�����A����̲׼v���I");

        Invoke(nameof(ResetStage), (float)clipFinal.length + 0.1f);
    }

    void UnlockInput()
    {
        isLocked = false;
        Debug.Log("�����J�A���\�U�@���q");
    }
    void HideVideo()
    {
        videoImage.color = Color.black;
        Debug.Log("�v�����񵲧��A�e���ܶ�");
    }
    void ResetStage()
    {
        currentIndex = 0;
        isLocked = false;
        videoImage.color = Color.black;
        Debug.Log("���m�i�סGcurrentIndex = 0�AisLocked = false");
    }
}
