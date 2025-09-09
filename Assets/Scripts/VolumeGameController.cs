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

    //// VolumeGameController.cs ���q�G�s�W���
    //[SerializeField] ParticleController particleController;

    // VolumeGameController.cs ���q�G�s�W���
    [SerializeField] SoundEffectManager soundEffectManager;

    // �ؼа϶��]���W�ƼҦ��ϥΡ^
    [SerializeField] Vector2 sizeRange;  // �̲�~�̲�
    [SerializeField] Vector2 speedRange;   // �̺C~�̧�

    // --- �ʲ�/�t�׬M�g�]�w ---
    [Header("dB �� �ʲ�/�t�׬M�g")]
    [SerializeField] float minDb;                  // ���W�ƤU��
    [SerializeField] float maxDb;                 // ���W�ƤW��

    [Header("���d�q�L����")]
    [Range(1, 11)] [SerializeField] private int requiredSuccessCount;  // �ݭn�s��X�����T�~��L��
    private int successCount = 1;                           // ��e�s�򥿽T����
    [SerializeField] Slider requiredSuccessSlider;
    private int RequiredSuccessCount => (int)requiredSuccessSlider.value == 1 ? 1 : ((int)requiredSuccessSlider.value - 1) * 10;
    [SerializeField] Text requiredSuccessValue;

    [Header("�y�{�]�w")]
    [SerializeField] private bool restartAfterFinal = false;
    // �Ŀ�G�̲׼v�������᭫�Y�}�l�F���ġG�������������d�]���µL�S�ġ^

    [Header("���ռҦ�")]
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
        //toleranceSlider.value = tolerance;
        //weightSlider.value = weight;
        //OnToleranceValueChange();
        //if (WebSocketClient.Instance != null)
        //    WebSocketClient.Instance.OnMessageReceived += HandleMessage;
        //ResetStage();
        // 1) ���M���J�� Invoke �P���񪬺A
        CancelInvoke();
        if (videoPlayer != null) { videoPlayer.Stop(); videoPlayer.clip = null; }
        if (videoImage != null) { videoImage.color = Color.black; }

        // 2) �����ƥ�]�קK���ƭq�\�^
        if (WebSocketClient.Instance != null)
            WebSocketClient.Instance.OnMessageReceived -= HandleMessage;

        // �P�B�Ʊ��r
        if (toleranceSlider != null) toleranceSlider.value = tolerance;
        if (weightSlider != null) weightSlider.value = weight;

        if (WebSocketClient.Instance != null)
            WebSocketClient.Instance.OnMessageReceived += HandleMessage;

        // 3) �_�쪬�A + UI/�S��
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
            Debug.Log("���b����v�����A�������������q��J");
            console.text = "���b����v�����A�������������q��J";
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
                Debug.Log($"����dB�G{dB}�A�g�L�[�v{WeightSliderValue}����{dBAfterWeight} dB�A��e���d{expected} dB�A�~�t���W�X�e��{toleranceSlider.value} dB�A�s�� {successCount} �����T�A�q�L");
                console.text = $"����dB�G{dB}�A�g�L�[�v{WeightSliderValue}����{dBAfterWeight} dB�A��e���d{expected} dB�A�~�t���W�X�e��{toleranceSlider.value} dB�A�s�� {successCount} �����T�A�q�L";
                PlayStageVideo(currentIndex);
                currentIndex++;
                successCount = 0; // ���m�U�@�����s�p��
            }
            else
            {
                Debug.Log($"����dB�G{dB}�A�g�L�[�v{WeightSliderValue}����{dBAfterWeight} dB�A��e���d{expected} dB�A�~�t���W�X�e��{toleranceSlider.value} dB�A�֭p {successCount} �����T");
                console.text = $"����dB�G{dB}�A�g�L�[�v{WeightSliderValue}����{dBAfterWeight} dB�A��e���d{expected} dB�A�~�t���W�X�e��{toleranceSlider.value} dB�A�֭p {successCount} �����T";
            }
        }
        else if (!isTestMode)
        {
            Debug.Log($"���� dB={dB}�A�g�L�[�v{WeightSliderValue}����{dBAfterWeight} dB�A���ثe�w���� {expected} dB�A�~�t�W�X�e��{toleranceSlider.value} dB�A����");
            console.text = $"���� dB={dB}�A�g�L�[�v{WeightSliderValue}����{dBAfterWeight} dB�A���ثe�w���� {expected} dB�A�~�t�W�X�e��{toleranceSlider.value} dB�A����";
            //successCount = 0; // �Y�n�令�֭p��ӫD�s���N���ѱ��o��
        }
        if (isTestMode && gameObject.activeSelf)
        {
            Debug.Log($"����ռҦ����� dB={dB}�A�g�L�[�v{WeightSliderValue}����{dBAfterWeight} dB");
            console.text = $"����ռҦ����� dB={dB}�A�g�L�[�v{WeightSliderValue}����{dBAfterWeight} dB";
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
            Debug.Log($"���񶥬q {index} �v���G{clip.name}");
            Invoke(nameof(ShowVideo), showVideoDelay);
            Invoke(nameof(HideVideo), (float)clip.length);
            Invoke(nameof(UnlockInput), (float)clip.length);


            if (clipFinal != null)
            {
                // �p�G�n����̫�v���A�̫ᶥ�q�A�����񧹳̫����d�v���~����̫�v��
                if (index == expectedVolumes.Length - 1)
                {
                    Invoke(nameof(PlayFinalVideo), (float)clip.length + 0.1f);
                }
            }
            else
            {
                // �p�G�����̫�v��
                if (index == expectedVolumes.Length - 1)
                {
                    //Invoke(nameof(ResetStage), (float)clip.length + 0.1f);  //  ������۰ʭ��ҹC��
                    Invoke(nameof(CloseStage), (float)clip.length + 0.1f);  //  �����ᤣ�۰ʭ��ҹC��
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
        Debug.Log("�T���q�����A����̲׼v���I");
        Invoke(nameof(ShowVideo), showVideoDelay);
        //Invoke(nameof(ResetStage), (float)clipFinal.length + 0.1f);

        // �v����������G�̳]�w�M�w Reset �� Close
        float endAt = (float)clipFinal.length + 0.05f;
        if (restartAfterFinal)
        {
            // ���Y�}�l�G�^���l���A�]�|��_�ɤl�B��� dB ���^
            Invoke(nameof(ResetStage), endAt);
        }
        else
        {
            // �������d�G���µL�S�� & ����J
            Invoke(nameof(CloseStage), endAt);
        }
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
            soundEffectManager.ResumeEmission();
            dbValue.gameObject.SetActive(true);
            //particleController.SetEmissionColor((ParticleController.EmissionColor)currentIndex);
        }
        Debug.Log("�v�����񵲧��A�e���ܶ�");
    }
    void ShowVideo()
    {
        videoImage.color = Color.white;
        soundEffectManager.PauseEmission();
        dbValue.gameObject.SetActive(false);
        Debug.Log("�v������}�l�A�e���ܥ�");
    }
    /// <summary>
    /// �������d�G�e�����¡B����v���e���B�����ɤl/�S�ġB���üƭ���ܡA������J�C
    /// </summary>
    public void CloseStage()
    {
        // �������ӥi�઺�Ƶ{�]�קK�ݯd Invoke �ɭP�S�Q��_ Show/Hide�^
        CancelInvoke();

        // ����J�A�קK����S�Y��T��
        isLocked = true;
        isStageClosed = true;

        // ��ı�G���¡B����v��
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.clip = null; // �i��G�M�� clip�A�O�ҧ����R�q
        }
        if (videoImage != null)
        {
            videoImage.color = Color.black;
        }

        // �S�ġG���������]�o�̥� Pause �N����o�g & ����ܡ^
        if (soundEffectManager != null)
        {
            soundEffectManager.PauseEmission();
        }

        // UI ��ܡG���� dB
        if (dbValue != null)
        {
            dbValue.gameObject.SetActive(false);
        }

        // �O�����A
        Debug.Log("[CloseStage] �w�������d�G�e�����¡B�L�S�ġB��J��w");
        if (console != null)
        {
            console.text = "���d�w�����]�e�����¡A�L�S�ġ^";
        }
    }

    void ResetStage()
    {
        currentIndex = 0;
        successCount = 0;   // �]�n�k�s
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
        Debug.Log("���m�i�סGcurrentIndex = 0�AisLocked = false");
        console.text = "�}�l��������";
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

    // --- ����Ĳ�o�T�ӭ��q���d ---
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
    /// �̥ؼ� dB �ȫإߤ@�� JSON �ᵹ HandleMessage�A�O���P WebSocket �ۦP�y�{�C
    /// </summary>
    private void SimulatePassForTarget(int targetDb)
    {
        //if (isLocked) return;

        //// �קK���H�s�A�ϱ��@�ӭ�l dB�A���[�v���n�O�ؼЭ�
        //float w = Mathf.Max(0.1f, weightSlider != null ? WeightSliderValue : 1f);
        //int injectedDb = Mathf.RoundToInt((float)targetDb / w);
        //Debug.Log($"{WeightSliderValue},{w},{injectedDb}");
        //// �� JSON
        //string json = "{\"dB\":\"" + injectedDb.ToString() + "\"}";

        //// �� requiredSuccessCount �s��e�J�h���A�O�Ҫ����L��
        //int times = Mathf.Max(1, RequiredSuccessCount);
        //for (int i = 0; i < times; i++)
        //{
        //    HandleMessage(json);
        //    if (isLocked) break; // �@���L���N�|�Q���A���ᤣ�ΦA�e
        //}

        if (isLocked || isStageClosed) return;
        if (currentIndex >= expectedVolumes.Length) return;

        int expected = expectedVolumes[currentIndex];
        if (expected != targetDb)
        {
            if (console != null) console.text = $"[TEST] �ثe���d�O {expected} dB�A���� {targetDb} dB";
            return;
        }
        soundEffectManager.SetAmplitude(targetDb);
        //// ����ثe�n�q�L�����d���ޡ]�Y�A�Q�j������N�O�d�F�Y�u���\�q�L��e���d�A�i��� currentIndex �ˬd�^
        //int targetIndex = System.Array.IndexOf(expectedVolumes, targetDb);
        //if (targetIndex < 0) return;

        //// �Y�Q�Y��u��q�L�u�ثe�Ҧb�����d�v�A�N�����U�@��ç�� if (targetIndex != currentIndex) return;
        //currentIndex = targetIndex;

        // ���������w�F���q�L����
        successCount = RequiredSuccessCount;

        // �������d�v���P�ի׫���y�{�]Show/Hide/Unlock/Final/Close ���b�o�̳B�z�^
        PlayStageVideo(currentIndex);

        // ���i��U�@���A�ç�s��p���k�s�A�P HandleMessage ���\����O���@�P
        currentIndex++;
        successCount = 0;

        // UI ���ܡ]�i��^
        if (console != null)
            console.text = $"[TEST] �����q�L {targetDb} dB ���d";
    }
}
