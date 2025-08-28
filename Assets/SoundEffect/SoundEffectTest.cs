using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEffectTest : MonoBehaviour
{
    public AudioEffectController audioEffectController;
    [Range(0f, 1f)][SerializeField] float volume = 0.5f;
    [Range(0f, 1f)][SerializeField] float amplitude = 0.1f;
    [Range(1f, 5f)][SerializeField] float frequency = 2.5f;
    public int SAMPLE_COUNT = 256; // �T�w 256 ��
    private float phase; // �H�u���סv�@���ۦ�
    // Start is called before the first frame update
    void Start()
    {
        
    }
    List<float> CreatSample(float amplitude, float frequency)
    {
        var samples = new List<float>(SAMPLE_COUNT);

        // �C�V��s�u�s��ۦ�v�A�T�O�e���|�y��
        // �ۦ�W�q = 2�k f �Gt
        phase += 2f * Mathf.PI * frequency * Time.deltaTime;

        // �N�ۦ쭭��b [0, 2�k) �H�קK�ܤӤj�y����״c��
        if (phase >= 2f * Mathf.PI) phase -= 2f * Mathf.PI;
        // �ΡGphase = Mathf.Repeat(phase, 2f * Mathf.PI);

        // �ͦ� 1 ���ת� 256 �����ˡ]���ä��G�b 0..1s�^
        float dt = 1f / SAMPLE_COUNT;
        for (int i = 0; i < SAMPLE_COUNT; i++)
        {
            float tLocal = i * dt; // 0..1s �������۹�ɶ�
            float value = amplitude * Mathf.Sin(phase + 2f * Mathf.PI * frequency * tLocal);
            samples.Add(value);
        }

        return samples;
    }
    // Update is called once per frame
    void Update()
    {
        audioEffectController.SetVolume(volume);

        audioEffectController.UpdateWaveform(CreatSample(amplitude, frequency));
    }
}
