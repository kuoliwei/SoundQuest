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

    public int SAMPLE_COUNT = 256; // 固定 256 筆
    //[Range(0f, (float)(2.0 * System.Math.PI))][SerializeField] float phase; // 以「弧度」作為相位
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
        // dB 例如 70、90、110… -> amplitude = dB/100
        // 你的 amplitude Range 是 [0, 2]，這裡做安全夾限
        amplitude = Mathf.Clamp(fDB / 100f, 0f, 2f);

        // volume 也一併對應到 0..1，用來驅動顏色（EnhancedGridWaveformVisualizer 會依 volume 染色）
        volume = Mathf.Clamp(fDB / 100f, 0f, 2f);
        volume = amplitude;

        // 頻率也隨音量大小變化
        float t = Mathf.InverseLerp(0f, 1.3f, amplitude);
        frequency = Mathf.Lerp(5f, 30f, t);

        viz.SetBottomLineHeight(fDB / 100f);
    }

    // === 新增：顯示/隱藏波形特效（取代粒子效果的 Pause/Resume） ===
    public void PauseEmission()
    {
        if (audioEffectController != null)
            audioEffectController.gameObject.SetActive(false);
        Debug.Log("[SoundEffectManager] 音波特效已暫停");
    }

    public void ResumeEmission()
    {
        if (audioEffectController != null)
            audioEffectController.gameObject.SetActive(true);
        Debug.Log("[SoundEffectManager] 音波特效已恢復");
    }
    List<float> CreatSample(float amplitude, float frequency)
    {
        var samples = new List<float>(SAMPLE_COUNT);

        // 每幀更新「連續相位」，確保畫面會流動
        // 相位增量 = 2π f Δt
        //phase += 2f * Mathf.PI * frequency * Time.deltaTime;

        // 將相位限制在 [0, 2π) 以避免變太大造成精度惡化
        //if (phase >= 2f * Mathf.PI) phase -= 2f * Mathf.PI;
        // 或：phase = Mathf.Repeat(phase, 2f * Mathf.PI);

        // 生成 1 秒跨度的 256 筆取樣（均勻分佈在 0..1s）
        float dt = 1f / SAMPLE_COUNT;
        for (int i = 0; i < SAMPLE_COUNT; i++)
        {
            float tLocal = i * dt; // 0..1s 之間的相對時間
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
