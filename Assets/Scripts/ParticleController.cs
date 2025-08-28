using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleController : MonoBehaviour
{
    public ParticleSystem ps;
    private Material mat;

    // �T���C��
    public enum EmissionColor { crimson, gold, green }
    private readonly Color crimson = new Color(0.86f, 0.08f, 0.24f);   // �����
    private readonly Color gold = new Color(1f, 0.843f, 0f);
    private readonly Color green = new Color(0.2f, 1f, 0.2f);          // �A���
    //private readonly Color cyanBlue = new Color(0f, 0.17f, 0.75f);     // �C�Ŧ�
    private Color currentColor;
    [HideInInspector] public Color CurrentColor => currentColor;
    private ParticleSystem.MainModule mainModule;

    [Header("��l�]�w")]
    public float intensity;
    public float defaultSize;  // Start Size
    public float defaultSpeed;   // Start Speed

    [Header("�U���O�@")]
    public float minSize;
    public float minSpeed;

    // �֨��}�C
    private ParticleSystem.Particle[] particleBuffer;

    void Start()
    {
        if (ps == null)
        {
            Debug.LogError("ParticleSystem �����w�I");
            return;
        }

        mainModule = ps.main;

        // ��l��
        mainModule.startSize = defaultSize;
        mainModule.startSpeed = defaultSpeed;

        mat = ps.GetComponent<ParticleSystemRenderer>().sharedMaterial;
        //mat.SetColor("_EmissionColor", baseColor * intensity);

        EnsureBuffer();
    }

    void Update()
    {
        //// ���ռ���
        //if (Input.GetKeyDown(KeyCode.KeypadMinus)) SetEmissionColor(-0.1f);
        //if (Input.GetKeyDown(KeyCode.KeypadPlus)) SetEmissionColor(0.1f);

        //if (Input.GetKeyDown(KeyCode.Alpha1)) AddTrailWidth(-0.01f);
        //if (Input.GetKeyDown(KeyCode.Alpha2)) AddTrailWidth(0.01f);

        //if (Input.GetKeyDown(KeyCode.Alpha3)) AddTrailSpeed(-0.1f);
        //if (Input.GetKeyDown(KeyCode.Alpha4)) AddTrailSpeed(0.1f);
    }

    public void SetEmissionColor(EmissionColor emissionColor)
    {
        Color color = Color.black;

        switch (emissionColor)
        {
            case EmissionColor.crimson:
                color = crimson; 
                break;
            case EmissionColor.gold:
                color = gold;
                break;
            case EmissionColor.green:
                color = green;
                break;
        }
        currentColor = color * intensity;
        // �ϥΥثe�� intensity
        mat.SetColor("_EmissionColor", color * intensity);
    }


    // --------------------
    // �[��覡�]�쥻���^
    // --------------------
    public void AddTrailWidth(float delta)
    {
        SetTrailWidthAbs(mainModule.startSize.constant + delta);
    }

    public void AddTrailSpeed(float delta)
    {
        SetTrailSpeedAbs(mainModule.startSpeed.constant + delta);
    }

    // --------------------
    // �������w����ȡ]�s��k�^
    // --------------------
    public void SetTrailWidthAbs(float newSize)
    {
        newSize = Mathf.Max(minSize, newSize);
        float oldSize = mainModule.startSize.constant;

        mainModule.startSize = newSize;

        float scale = (oldSize <= 0f) ? 1f : (newSize / oldSize);
        if (!Mathf.Approximately(scale, 1f))
            ScaleAliveParticleSize(scale);

        Debug.Log($"[Set] Trail �e��: {oldSize:F3} -> {newSize:F3}");
    }

    public void SetTrailSpeedAbs(float newSpeed)
    {
        newSpeed = Mathf.Max(minSpeed, newSpeed);
        float oldSpeed = mainModule.startSpeed.constant;

        mainModule.startSpeed = newSpeed;

        float scale = (oldSpeed == 0f) ? 1f : (newSpeed / oldSpeed);
        if (!Mathf.Approximately(scale, 1f))
            ScaleAliveParticleVelocity(scale);

        Debug.Log($"[Set] �ɤl�t��: {oldSpeed:F2} -> {newSpeed:F2}");
    }

    // ===== �u�� =====
    private void ScaleAliveParticleSize(float scale)
    {
        EnsureBuffer();
        int alive = ps.GetParticles(particleBuffer);
        for (int i = 0; i < alive; i++)
            particleBuffer[i].startSize *= scale;
        ps.SetParticles(particleBuffer, alive);
    }

    private void ScaleAliveParticleVelocity(float scale)
    {
        EnsureBuffer();
        int alive = ps.GetParticles(particleBuffer);
        for (int i = 0; i < alive; i++)
            particleBuffer[i].velocity *= scale;
        ps.SetParticles(particleBuffer, alive);
    }

    private void EnsureBuffer()
    {
        int need = Mathf.Max(256, mainModule.maxParticles);
        if (particleBuffer == null || particleBuffer.Length < need)
            particleBuffer = new ParticleSystem.Particle[need];
    }
    public void PauseEmission()
    {
        if (ps == null) return;

        ps.Stop();
        ps.Clear(); // �i��G���w�g�X�{���ɤl�ߧY����
        Debug.Log("[ParticleController] �ɤl�ĪG�w�Ȱ�");
    }

    public void ResumeEmission()
    {
        if (ps == null) return;

        ps.Play();
        Debug.Log("[ParticleController] �ɤl�ĪG�w��_");
    }
}
