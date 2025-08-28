using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundEffectTest : MonoBehaviour
{
    public AudioEffectController audioEffectController;
    [Range(0f, 1f)][SerializeField] float volume = 0.5f;
    [Range(0f, 1f)][SerializeField] float amplitude = 0.1f;
    [Range(1f, 5f)][SerializeField] float frequency = 2.5f;
    public int SAMPLE_COUNT = 256; // 固定 256 筆
    private float phase; // 以「弧度」作為相位
    // Start is called before the first frame update
    void Start()
    {
        
    }
    List<float> CreatSample(float amplitude, float frequency)
    {
        var samples = new List<float>(SAMPLE_COUNT);

        // 每幀更新「連續相位」，確保畫面會流動
        // 相位增量 = 2π f Δt
        phase += 2f * Mathf.PI * frequency * Time.deltaTime;

        // 將相位限制在 [0, 2π) 以避免變太大造成精度惡化
        if (phase >= 2f * Mathf.PI) phase -= 2f * Mathf.PI;
        // 或：phase = Mathf.Repeat(phase, 2f * Mathf.PI);

        // 生成 1 秒跨度的 256 筆取樣（均勻分佈在 0..1s）
        float dt = 1f / SAMPLE_COUNT;
        for (int i = 0; i < SAMPLE_COUNT; i++)
        {
            float tLocal = i * dt; // 0..1s 之間的相對時間
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
