using UnityEngine;
using System.Collections.Generic;

namespace AIStageBGApp
{
    public class AudioVisualizer : AudioEffectController
    {
        public LineRenderer lineRenderer;
        public int pointCount = 256;

        public override void UpdateWaveform(List<float> samples)
        {
            base.UpdateWaveform(samples);
            if (samples == null || samples.Count < pointCount) return;
            if(lineRenderer != null)
            {

                lineRenderer.positionCount = pointCount;
                for (int i = 0; i < pointCount; i++)
                {
                    float x = i * 1.0f / pointCount;
                    float y = samples[i];
                    lineRenderer.SetPosition(i, new Vector3(x, y, 0));
                }
            }
        }
    }

}