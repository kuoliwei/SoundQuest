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
        if (Mathf.Abs(dB - expected) <= 5)
        {
            PlayStageVideo(currentIndex);
            currentIndex++;
        }
        else
        {
            Debug.Log($"收到 dB={dB}，但目前預期為 {expected}，忽略");
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
            Debug.Log($"播放階段 {index} 影片：{clip.name}");
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
        videoImage.color = Color.white;
        videoPlayer.Stop();
        videoPlayer.clip = clipFinal;
        videoPlayer.Play();
        Debug.Log("三階段完成，播放最終影片！");

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
    void ResetStage()
    {
        currentIndex = 0;
        isLocked = false;
        videoImage.color = Color.black;
        Debug.Log("重置進度：currentIndex = 0，isLocked = false");
    }
}
