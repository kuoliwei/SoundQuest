using UnityEngine;
using System.Collections.Generic;

public class AudioEffectController : MonoBehaviour
{
    //public AddEffectConfig addEffectConfig;
    void Start()
    {
        
    }
    public virtual void SetVolume(float volume, bool smooth = true)
    {
        
    }
    public virtual void UpdateWaveform(List<float> samples)
    {
        if (!gameObject.active)
        {
            return; 
        }

    }

}
