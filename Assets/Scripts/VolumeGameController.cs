using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using static ParticleController;

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

    // VolumeGameController.cs ���q�G�s�W���
    [SerializeField] ParticleController particleController;

    // �ؼа϶��]���W�ƼҦ��ϥΡ^
    [SerializeField] Vector2 sizeRange;  // �̲�~�̲�
    [SerializeField] Vector2 speedRange;   // �̺C~�̧�

    // --- �ʲ�/�t�׬M�g�]�w ---
    [Header("dB �� �ʲ�/�t�׬M�g")]
    [SerializeField] float minDb;                  // ���W�ƤU��
    [SerializeField] float maxDb;                 // ���W�ƤW��

    [Header("���d�q�L����")]
    [SerializeField] private int requiredSuccessCount;  // �ݭn�s��X�����T�~��L��
    private int successCount = 0;                           // ��e�s�򥿽T����

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
                Debug.Log($"����dB�G{dB}�A�g�L�[�v{weightSlider.value}����{dBAfterWeight} dB�A��e���d{expected} dB�A�~�t���W�X�e��{toleranceSlider.value} dB�A�s�� {successCount} �����T�A�q�L");
                console.text = $"����dB�G{dB}�A�g�L�[�v{weightSlider.value}����{dBAfterWeight} dB�A��e���d{expected} dB�A�~�t���W�X�e��{toleranceSlider.value} dB�A�s�� {successCount} �����T�A�q�L";
                PlayStageVideo(currentIndex);
                currentIndex++;
                successCount = 0; // ���m�U�@�����s�p��
            }
            else
            {
                Debug.Log($"����dB�G{dB}�A�g�L�[�v{weightSlider.value}����{dBAfterWeight} dB�A��e���d{expected} dB�A�~�t���W�X�e��{toleranceSlider.value} dB�A�s�� {successCount} �����T");
                console.text = $"����dB�G{dB}�A�g�L�[�v{weightSlider.value}����{dBAfterWeight} dB�A��e���d{expected} dB�A�~�t���W�X�e��{toleranceSlider.value} dB�A�s�� {successCount} �����T";
            }
        }
        else
        {
            Debug.Log($"���� dB={dB}�A�g�L�[�v{weightSlider.value}����{dBAfterWeight} dB�A���ثe�w���� {expected} dB�A�~�t�W�X�e��{toleranceSlider.value} dB�A����");
            console.text = $"���� dB={dB}�A�g�L�[�v{weightSlider.value}����{dBAfterWeight} dB�A���ثe�w���� {expected} dB�A�~�t�W�X�e��{toleranceSlider.value} dB�A����";
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
            Debug.Log($"���񶥬q {index} �v���G{clip.name}");
            Invoke(nameof(ShowVideo), showVideoDelay);
            Invoke(nameof(HideVideo), (float)clip.length);
            Invoke(nameof(UnlockInput), (float)clip.length);

            //// �p�G�O�̫ᶥ�q�A�����񧹤~���񧹦��v��
            //if (index == expectedVolumes.Length - 1)
            //{
            //    Invoke(nameof(PlayFinalVideo), (float)clip.length + 0.1f);
            //}

            // �p�G�����̫�v��
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
        Debug.Log("�T���q�����A����̲׼v���I");
        Invoke(nameof(ShowVideo), showVideoDelay);
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
        if(currentIndex <= 2)
        {
            particleController.ResumeEmission();
            dbValue.gameObject.SetActive(true);
            particleController.SetEmissionColor((ParticleController.EmissionColor)currentIndex);
        }
        Debug.Log("�v�����񵲧��A�e���ܶ�");
    }
    void ShowVideo()
    {
        videoImage.color = Color.white;
        particleController.PauseEmission();
        dbValue.gameObject.SetActive(false);
        Debug.Log("�v������}�l�A�e���ܥ�");
    }
    void ResetStage()
    {
        currentIndex = 0;
        successCount = 0;   // �]�n�k�s
        isLocked = false;
        videoImage.color = Color.black;
        particleController.ResumeEmission();
        dbValue.gameObject.SetActive(true);
        particleController.SetEmissionColor((ParticleController.EmissionColor)currentIndex);
        Debug.Log("���m�i�סGcurrentIndex = 0�AisLocked = false");
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
