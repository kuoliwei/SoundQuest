using AIStageBGApp;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEffectManager : MonoBehaviour
{
    public AudioEffectController audioEffectController;
    [Range(0f, 2f)][SerializeField] float volume = 0f;
    [Range(0f, 2f)][SerializeField] float amplitude = 0f;
    [Range(1f, 3f)][SerializeField] float ampScale = 2f;
    [SerializeField] int ampPower = 3;
    [Range(1f, 100f)][SerializeField] float frequency = 20f;

    public int SAMPLE_COUNT = 256; // �T�w 256 ��
    //[Range(0f, (float)(2.0 * System.Math.PI))][SerializeField] float phase; // �H�u���סv�@���ۦ�
    private float phase;
    EnhancedGridWaveformVisualizer viz;
    // Start is called before the first frame update
    void Start()
    {
        viz = audioEffectController as EnhancedGridWaveformVisualizer;
    }
    public void SetAmplitude(int dB)
    {
        float fDB = (float)dB;
        // dB �Ҧp 70�B90�B110�K -> amplitude = dB/100
        // �A�� amplitude Range �O [0, 2]�A�o�̰��w������
        amplitude = Mathf.Clamp(fDB / 100f, 0f, 2f);

        // volume �]�@�ֹ����� 0..1�A�Ψ��X���C��]EnhancedGridWaveformVisualizer �|�� volume �V��^
        volume = Mathf.Clamp(fDB / 100f, 0f, 2f);
        volume = amplitude;

        // �W�v�]�H���q�j�p�ܤ�
        float t = Mathf.InverseLerp(0f, 1.3f, amplitude);
        frequency = Mathf.Lerp(5f, 30f, t);

        viz.SetBottomLineHeight(fDB / 100f);
    }

    // === �s�W�G���/���êi�ίS�ġ]���N�ɤl�ĪG�� Pause/Resume�^ ===
    public void PauseEmission()
    {
        if (audioEffectController != null)
            audioEffectController.gameObject.SetActive(false);
        Debug.Log("[SoundEffectManager] ���i�S�Ĥw�Ȱ�");
    }

    public void ResumeEmission()
    {
        if (audioEffectController != null)
            audioEffectController.gameObject.SetActive(true);
        Debug.Log("[SoundEffectManager] ���i�S�Ĥw��_");
    }
    List<float> CreatSample(float amplitude, float frequency)
    {
        var samples = new List<float>(SAMPLE_COUNT);

        // �C�V��s�u�s��ۦ�v�A�T�O�e���|�y��
        // �ۦ�W�q = 2�k f �Gt
        //phase += 2f * Mathf.PI * frequency * Time.deltaTime;

        // �N�ۦ쭭��b [0, 2�k) �H�קK�ܤӤj�y����״c��
        //if (phase >= 2f * Mathf.PI) phase -= 2f * Mathf.PI;
        // �ΡGphase = Mathf.Repeat(phase, 2f * Mathf.PI);

        // �ͦ� 1 ���ת� 256 �����ˡ]���ä��G�b 0..1s�^
        float dt = 1f / SAMPLE_COUNT;
        for (int i = 0; i < SAMPLE_COUNT; i++)
        {
            float tLocal = i * dt; // 0..1s �������۹�ɶ�
            float value = amplitude * Mathf.Sin(phase + 2f * Mathf.PI * frequency * tLocal);
            //samples.Add(value * GetScale(i, ampScale, ampPower));
            samples.Add(value);
            //samples.Add(value);
        }

        return samples;
    }
    float GetScale(int i, float scale, int power)
    {
        float half = (float)SAMPLE_COUNT / 2f;
        if (i <= half)
        {
            return Mathf.Pow((float)i / half, power) * scale;
        }
        else
        {
            return Mathf.Pow((float)(SAMPLE_COUNT - i) / half, power) * scale;
        }
        //return (float)(SAMPLE_COUNT - i) / (float)SAMPLE_COUNT * scale;
    }
    // Update is called once per frame
    void Update()
    {
        if (viz != null && viz.IsInitialized)
        {
            viz.SetVolume(volume);
            viz.UpdateWaveform(CreatSample(amplitude, Random.Range(1f, frequency)));
        }
    }
}
