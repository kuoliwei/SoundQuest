using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Device;
namespace AIStageBGApp
{

    public class EnhancedGridWaveformVisualizer : AudioVisualizer
    {
        [Header("Position Settings")]
        [SerializeField] private Vector3 visualizerPosition = Vector3.zero;
        [SerializeField] private Vector3 visualizerRotation = Vector3.zero;
        [SerializeField] private Vector3 visualizerScale = Vector3.one;
        [SerializeField] private bool updatePositionInRealtime = true;

        [Header("Visualization Settings")]
        [SerializeField] private int visualizationPoints = 256;
        [SerializeField] private float visualizationHeight = 5f;
        [SerializeField] private float visualizationWidth = 20f;
        [SerializeField] private float smoothSpeed = 10f;

        [Header("Visual Style")]
        [SerializeField] private Color lineColor = Color.white;
        [SerializeField] private float horizontalLineWidth = 0.1f;
        [SerializeField] private float verticalLineWidth = 0.05f;
        [SerializeField] private bool mirrorWaveform = false;
        [SerializeField] private bool addVerticalGridLines = true;
        [SerializeField] private int gridDensity = 1; // 1 = every point, 2 = every other point, etc.


        [Header("Glow Effect")]
        [SerializeField] private bool useGlowEffect = true;
        [SerializeField] private Color glowColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private float glowIntensity = 2.0f;
        [SerializeField] private Material glowMaterial;
        [SerializeField] private bool pulseGlow = true;
        [SerializeField] private float pulseSpeed = 1.0f;
        [SerializeField] private float minGlowIntensity = 1.0f;
        [SerializeField] private float maxGlowIntensity = 3.0f;
        [SerializeField] private bool isURP = true; // Flag to indicate if using URP

        [Header("Frequency Balance")]
        [SerializeField] private bool balanceFrequencies = false;
        [SerializeField] private float lowFrequencyAttenuation = 0.5f;
        [SerializeField] private float highFrequencyBoost = 2.0f;
        [SerializeField] private AnimationCurve frequencyWeightCurve;
        [SerializeField] private bool useLogarithmicDistribution = true;
        [SerializeField] private float balanceStrength = 1.0f;

        // Container objects
        private GameObject visualizerContainer;
        private GameObject linesContainer;

        // Line renderers
        private LineRenderer topLineRenderer;
        private LineRenderer bottomLineRenderer;
        private LineRenderer[] verticalLineRenderers;
        private LineRenderer topTriangleLineRenderer;
        private LineRenderer bottomTriangleLineRenderer;

        // Effect Color
        private readonly Color white = Color.white;
        private readonly Color crimson = new Color(0.86f, 0.08f, 0.24f);   // 血紅色
        private readonly Color gold = new Color(1f, 0.843f, 0f);
        private readonly Color green = new Color(0.2f, 1f, 0.2f);          // 翠綠色
        private readonly Color ceil = new Color(0.5f, 0f, 0.5f);
        [SerializeField, Range(0f, 3f)] private float brightness = 2f; // 亮度

        // For glow effect
        private float glowTime = 0f;
        private Material defaultGlowMaterial;

        // Position arrays
        private Vector3[] topLinePositions;
        private Vector3[] bottomLinePositions;
        private Vector3[] targetTopPositions;
        private Vector3[] targetBottomPositions;

        // Volume-based color control
        private float currentVolume = 0f;
        private Color targetColor;
        private float colorSmoothSpeed = 5f;

        public float cropdisplay = 1f;

        // 欄位
        public bool IsInitialized { get; private set; }

        private void Start()
        {
            // Initialize frequency weight curve if not set
            if (frequencyWeightCurve == null || frequencyWeightCurve.length == 0)
            {
                frequencyWeightCurve = new AnimationCurve();
                frequencyWeightCurve.AddKey(0f, 1f);
                frequencyWeightCurve.AddKey(0.2f, 1.2f);
                frequencyWeightCurve.AddKey(0.4f, 1.5f);
                frequencyWeightCurve.AddKey(0.6f, 1.8f);
                frequencyWeightCurve.AddKey(0.8f, 2.2f);
                frequencyWeightCurve.AddKey(1f, 2.5f);
            }

            // Create container objects
            visualizerContainer = new GameObject("VisualizerContainer");
            visualizerContainer.transform.SetParent(transform, false);

            linesContainer = new GameObject("LinesContainer");
            linesContainer.transform.SetParent(visualizerContainer.transform, false);

            // Set initial position, rotation, and scale
            UpdateContainerTransform();

            // Initialize arrays
            topLinePositions = new Vector3[visualizationPoints];
            bottomLinePositions = new Vector3[visualizationPoints];
            targetTopPositions = new Vector3[visualizationPoints];
            targetBottomPositions = new Vector3[visualizationPoints];

            // Create top line renderer
            GameObject topLineObj = new GameObject("TopWaveform");
            topLineObj.transform.SetParent(linesContainer.transform, false);
            topLineRenderer = topLineObj.AddComponent<LineRenderer>();
            topLineRenderer.positionCount = visualizationPoints;
            topLineRenderer.startWidth = horizontalLineWidth;
            topLineRenderer.endWidth = horizontalLineWidth;

            ApplyGlowIfAvaible(topLineRenderer);
            topLineRenderer.useWorldSpace = false; // Use local space

            // Create bottom line renderer
            GameObject bottomLineObj = new GameObject("BottomWaveform");
            bottomLineObj.transform.SetParent(linesContainer.transform, false);
            bottomLineRenderer = bottomLineObj.AddComponent<LineRenderer>();
            bottomLineRenderer.positionCount = visualizationPoints;
            bottomLineRenderer.startWidth = horizontalLineWidth;
            bottomLineRenderer.endWidth = horizontalLineWidth;
            ApplyGlowIfAvaible(bottomLineRenderer);


            bottomLineRenderer.useWorldSpace = false; // Use local space

            // Create vertical line renderers
            int verticalLineCount = addVerticalGridLines ? visualizationPoints / gridDensity : 0;
            verticalLineRenderers = new LineRenderer[verticalLineCount];

            for (int i = 0; i < verticalLineCount; i++)
            {
                int pointIndex = i * gridDensity;
                if (pointIndex < visualizationPoints)
                {
                    GameObject verticalLineObj = new GameObject("VerticalLine_" + pointIndex);
                    verticalLineObj.transform.SetParent(linesContainer.transform, false);
                    verticalLineRenderers[i] = verticalLineObj.AddComponent<LineRenderer>();
                    verticalLineRenderers[i].positionCount = 2; // Top and bottom points
                    verticalLineRenderers[i].startWidth = verticalLineWidth;
                    verticalLineRenderers[i].endWidth = verticalLineWidth;


                    ApplyGlowIfAvaible(verticalLineRenderers[i]);

                    verticalLineRenderers[i].useWorldSpace = false; // Use local space
                }
            }

            // Set initial positions
            for (int i = 0; i < visualizationPoints; i++)
            {
                float xPos = (i / (float)(visualizationPoints - 1)) * visualizationWidth - (visualizationWidth / 2);

                // Set initial positions at the center line
                topLinePositions[i] = new Vector3(xPos, 0, 0);
                bottomLinePositions[i] = new Vector3(xPos, 0, 0);
                targetTopPositions[i] = topLinePositions[i];
                targetBottomPositions[i] = bottomLinePositions[i];

                // Update line renderers
                topLineRenderer.SetPosition(i, topLinePositions[i]);
                bottomLineRenderer.SetPosition(i, bottomLinePositions[i]);
            }

            // Initialize target color to current line color
            targetColor = lineColor;

            // Update vertical line renderers
            UpdateVerticalLineRenderers();

            // Start() 結尾（已建立 arrays/LineRenderer 後）
            IsInitialized = true;
        }
        private void ApplyGlowIfAvaible(LineRenderer lineRenderer)
        {
            // Apply glow material if enabled
            if (useGlowEffect)
            {

                if (glowMaterial == null)
                {
                    glowMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                }

                lineRenderer.material = glowMaterial;
                SetGlowColor(lineRenderer, glowColor, glowIntensity);
            }
            else
            {
                // Use URP compatible shader if in URP mode
                if (isURP)
                {
                    lineRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                }
                else
                {
                    lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                }
                lineRenderer.startColor = lineColor;
                lineRenderer.endColor = lineColor;
            }
        }

        private Material CreateGlowMaterial()
        {
            Material material;

            if (isURP)
            {
                // Create URP compatible material
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    Debug.LogError("URP Unlit shader not found! Make sure URP is properly installed.");
                    // Fallback to a simple material
                    material = new Material(Shader.Find("Hidden/InternalErrorShader"));
                }
                else
                {
                    material = new Material(shader);
                    material.SetColor("_BaseColor", glowColor * glowIntensity);

                    // Try to find and use URP particles shader for better glow
                    Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                    if (particleShader != null)
                    {
                        Material particleMat = new Material(particleShader);
                        particleMat.SetColor("_BaseColor", glowColor * glowIntensity);
                        particleMat.SetFloat("_Surface", 1); // Transparent
                        particleMat.SetFloat("_Blend", 0);  // Alpha
                        particleMat.EnableKeyword("_EMISSION");

                        if (particleMat.HasProperty("_EmissionColor"))
                        {
                            particleMat.SetColor("_EmissionColor", glowColor * glowIntensity);
                            return particleMat;
                        }
                    }
                }
            }
            else
            {
                // Original built-in RP material
                material = new Material(Shader.Find("Standard"));
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", glowColor * glowIntensity);
                material.SetFloat("_Glossiness", 0); // No glossiness for better glow effect
                material.SetColor("_Color", glowColor);

                // For better glow, you can also use a custom shader if available
                // If you have the "Particles/Additive" shader, it works well for glow
                Shader additiveShader = Shader.Find("Particles/Additive");
                if (additiveShader != null)
                {
                    material = new Material(additiveShader);
                    material.SetColor("_TintColor", glowColor * glowIntensity * 0.5f);
                }
            }

            return material;
        }

        private void SetGlowColor(LineRenderer lineRenderer, Color color, float intensity)
        {
            if (lineRenderer == null || lineRenderer.material == null)
                return;

            Material mat = lineRenderer.material;

            if (isURP)
            {
                // URP material properties
                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", color * intensity);
                }

                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color * intensity);
                }
            }
            else
            {
                // Built-in RP material properties
                if (mat.HasProperty("_EmissionColor"))
                {
                    // Standard shader with emission
                    mat.SetColor("_EmissionColor", color * intensity);
                    mat.SetColor("_Color", color);
                }
                else if (mat.HasProperty("_TintColor"))
                {
                    // Particles/Additive shader
                    mat.SetColor("_TintColor", color * intensity * 0.5f);
                }
                else
                {
                    // Fallback to setting the color directly
                    lineRenderer.startColor = color * intensity;
                    lineRenderer.endColor = color * intensity;
                }
            }
        }

        private void UpdateGlowEffect() // 不做歸一化
        {
            if (!useGlowEffect || !pulseGlow)
                return;

            glowTime += Time.deltaTime * pulseSpeed;
            float pulseValue = Mathf.PingPong(glowTime, 1.0f);
            float currentIntensity = Mathf.Lerp(minGlowIntensity, maxGlowIntensity, pulseValue);

            // Update all line renderers with current color (which may be affected by volume)
            if (topLineRenderer != null)
                SetGlowColor(topLineRenderer, glowColor, currentIntensity);

            if (bottomLineRenderer != null)
                SetGlowColor(bottomLineRenderer, glowColor, currentIntensity);

            if (topTriangleLineRenderer != null)
                SetGlowColor(topTriangleLineRenderer, glowColor, currentIntensity);

            if (bottomTriangleLineRenderer != null)
                SetGlowColor(bottomTriangleLineRenderer, glowColor, currentIntensity);

            // Update vertical line renderers
            if (verticalLineRenderers != null)
            {
                foreach (LineRenderer lr in verticalLineRenderers)
                {
                    if (lr != null)
                        SetGlowColor(lr, glowColor, currentIntensity);
                }
            }
        }

        private void UpdateContainerTransform()
        {
            if (visualizerContainer != null)
            {
                visualizerContainer.transform.localPosition = visualizerPosition;
                visualizerContainer.transform.localRotation = Quaternion.Euler(visualizerRotation);
                visualizerContainer.transform.localScale = visualizerScale;
            }
        }

        private void UpdateVerticalLineRenderers()
        {
            if (!addVerticalGridLines || verticalLineRenderers == null)
                return;

            for (int i = 0; i < verticalLineRenderers.Length; i++)
            {
                int pointIndex = i * gridDensity;
                if (pointIndex < visualizationPoints && verticalLineRenderers[i] != null)
                {
                    verticalLineRenderers[i].SetPosition(0, topLinePositions[pointIndex]);
                    verticalLineRenderers[i].SetPosition(1, bottomLinePositions[pointIndex]);
                }
            }
        }

        public void UpdateVisualization(List<float> samples)
        {
            if (samples == null || samples.Count == 0)
                return;

            for (int i = 0; i < visualizationPoints; i++)
            {
                float xPos = (i / (float)(visualizationPoints - 1)) * visualizationWidth - (visualizationWidth / 2);
                float crop = visualizationPoints * cropdisplay;
                //float crop = visualizationPoints;

                if (i < crop)
                {
                    float value = 0;
                    int sampleIndex;

                    if (balanceFrequencies)
                    {
                        // Calculate normalized position (0-1) from left to right
                        float position = i / (float)(visualizationPoints - 1);

                        if (useLogarithmicDistribution)
                        {
                            // Logarithmic distribution gives more space to lower frequencies
                            float logPosition = Mathf.Pow(position, 0.5f); // Square root for mild logarithmic effect
                            sampleIndex = Mathf.FloorToInt(logPosition * (samples.Count - 1));
                        }
                        else
                        {
                            // Linear distribution
                            sampleIndex = Mathf.FloorToInt(position * (samples.Count - 1));
                        }

                        if (sampleIndex < samples.Count)
                        {
                            // Get the raw sample value
                            value = samples[sampleIndex];

                            // Apply frequency-based weighting
                            float weight = 1.0f;

                            // Use weight curve if available
                            if (frequencyWeightCurve != null && frequencyWeightCurve.length > 0)
                            {
                                weight = frequencyWeightCurve.Evaluate(position);
                            }
                            else
                            {
                                // Apply manual frequency balancing
                                if (position < 0.3f)
                                {
                                    // Attenuate low frequencies (left side)
                                    weight = Mathf.Lerp(lowFrequencyAttenuation, 1.0f, position / 0.3f);
                                }
                                else
                                {
                                    // Boost high frequencies (right side)
                                    weight = Mathf.Lerp(1.0f, highFrequencyBoost, (position - 0.3f) / 0.7f);
                                }
                            }

                            // Apply the weight with balance strength
                            weight = 1.0f + (weight - 1.0f) * balanceStrength;
                            value *= weight;

                            // 拿掉規一化，直接放大
                            value *= visualizationHeight;
                        }
                    }
                    else
                    {
                        // Original method - direct sample mapping
                        int samplesPerPoint = Mathf.Max(1, samples.Count / visualizationPoints);
                        sampleIndex = i * samplesPerPoint;

                        if (sampleIndex < samples.Count)
                        {
                            // Use the raw sample value (can be positive or negative)
                            value = samples[sampleIndex] * visualizationHeight;
                        }
                    }

                    // Update target positions
                    targetTopPositions[i] = new Vector3(xPos, Mathf.Abs(value), 0); // Always positive for top

                    if (mirrorWaveform)
                    {
                        targetBottomPositions[i] = new Vector3(xPos, -Mathf.Abs(value), 0); // Always negative for bottom (mirrored)
                    }
                    else
                    {
                        targetBottomPositions[i] = new Vector3(xPos, -value, 0); // Inverse of top (follows waveform)
                    }
                }
                else
                {
                    targetTopPositions[i] = new Vector3(0, 0, 0);
                    targetBottomPositions[i] = new Vector3(0, 0, 0);
                }
            }
        }

        //public void UpdateVisualization(List<float> samples) // 做歸一化
        //{
        //    if (samples == null || samples.Count == 0)
        //        return;

        //    // Find max amplitude for normalization
        //    float maxAmplitude = 0.01f; // Minimum to avoid division by zero
        //    for (int i = 0; i < samples.Count; i++)
        //    {
        //        float amplitude = Mathf.Abs(samples[i]);
        //        if (amplitude > maxAmplitude)
        //        {
        //            maxAmplitude = amplitude;
        //        }
        //    }


        //    for (int i = 0; i < visualizationPoints; i++)
        //    {
        //        float xPos = (i / (float)(visualizationPoints - 1)) * visualizationWidth - (visualizationWidth / 2);
        //        float crop = visualizationPoints* cropdisplay;

        //        if (i < crop)
        //        {
                    

        //            // Get sample for this point with improved distribution
        //            float value = 0;
        //            int sampleIndex;

        //            if (balanceFrequencies)
        //            {
        //                // Calculate normalized position (0-1) from left to right
        //                float position = i / (float)(visualizationPoints - 1);

        //                if (useLogarithmicDistribution)
        //                {
        //                    // Logarithmic distribution gives more space to lower frequencies
        //                    float logPosition = Mathf.Pow(position, 0.5f); // Square root for mild logarithmic effect
        //                    sampleIndex = Mathf.FloorToInt(logPosition * (samples.Count - 1));
        //                }
        //                else
        //                {
        //                    // Linear distribution
        //                    sampleIndex = Mathf.FloorToInt(position * (samples.Count - 1));
        //                }

        //                if (sampleIndex < samples.Count)
        //                {
        //                    // Get the raw sample value
        //                    value = samples[sampleIndex];

        //                    // Apply frequency-based weighting
        //                    float weight = 1.0f;

        //                    // Use weight curve if available
        //                    if (frequencyWeightCurve != null && frequencyWeightCurve.length > 0)
        //                    {
        //                        weight = frequencyWeightCurve.Evaluate(position);
        //                    }
        //                    else
        //                    {
        //                        // Apply manual frequency balancing
        //                        if (position < 0.3f)
        //                        {
        //                            // Attenuate low frequencies (left side)
        //                            weight = Mathf.Lerp(lowFrequencyAttenuation, 1.0f, position / 0.3f);
        //                        }
        //                        else
        //                        {
        //                            // Boost high frequencies (right side)
        //                            weight = Mathf.Lerp(1.0f, highFrequencyBoost, (position - 0.3f) / 0.7f);
        //                        }
        //                    }

        //                    // Apply the weight with balance strength
        //                    weight = 1.0f + (weight - 1.0f) * balanceStrength;
        //                    value *= weight;

        //                    // Normalize if needed
        //                    if (maxAmplitude > 0.01f)
        //                    {
        //                        value = value / maxAmplitude * visualizationHeight;
        //                    }
        //                    else
        //                    {
        //                        value *= visualizationHeight;
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                // Original method - direct sample mapping
        //                int samplesPerPoint = Mathf.Max(1, samples.Count / visualizationPoints);
        //                sampleIndex = i * samplesPerPoint;

        //                if (sampleIndex < samples.Count)
        //                {
        //                    // Use the raw sample value (can be positive or negative)
        //                    value = samples[sampleIndex] * visualizationHeight;
        //                }
        //            }

        //            // Update target positions
        //            targetTopPositions[i] = new Vector3(xPos, Mathf.Abs(value), 0); // Always positive for top

        //            if (mirrorWaveform)
        //            {
        //                targetBottomPositions[i] = new Vector3(xPos, -Mathf.Abs(value), 0); // Always negative for bottom (mirrored)
        //            }
        //            else
        //            {
        //                targetBottomPositions[i] = new Vector3(xPos, -value, 0); // Inverse of top (follows waveform)
        //            }
        //        }
        //        else
        //        {
        //            targetTopPositions[i] = new Vector3(0, 0, 0); // Always positive for top
        //            targetBottomPositions[i] = new Vector3(0, 0, 0); // Inverse of top (follows waveform)
        //        }
                
        //    }
        //}

        private void Update()
        {
            // Update container transform if needed
            if (updatePositionInRealtime)
            {
                UpdateContainerTransform();
            }

            // Smoothly move current positions toward target positions
            for (int i = 0; i < visualizationPoints; i++)
            {
                // Update top positions
                topLinePositions[i] = Vector3.Lerp(topLinePositions[i], targetTopPositions[i], Time.deltaTime * smoothSpeed);

                // Update bottom positions
                bottomLinePositions[i] = Vector3.Lerp(bottomLinePositions[i], targetBottomPositions[i], Time.deltaTime * smoothSpeed);

                // Update line renderers
                topLineRenderer.SetPosition(i, topLinePositions[i]);
                bottomLineRenderer.SetPosition(i, bottomLinePositions[i]);
            }

            // Smoothly transition colors based on volume
            if (useGlowEffect)
            {
                glowColor = Color.Lerp(glowColor, targetColor, Time.deltaTime * colorSmoothSpeed);
                UpdateGlowEffect();
            }
            else
            {
                lineColor = Color.Lerp(lineColor, targetColor, Time.deltaTime * colorSmoothSpeed);

                // Update line renderer colors
                if (topLineRenderer != null)
                {
                    topLineRenderer.startColor = lineColor;
                    topLineRenderer.endColor = lineColor;
                }

                if (bottomLineRenderer != null)
                {
                    bottomLineRenderer.startColor = lineColor;
                    bottomLineRenderer.endColor = lineColor;
                }

                // Update vertical line renderers
                if (verticalLineRenderers != null)
                {
                    foreach (LineRenderer lr in verticalLineRenderers)
                    {
                        if (lr != null)
                        {
                            lr.startColor = lineColor;
                            lr.endColor = lineColor;
                        }
                    }
                }
            }

            // Update vertical line renderers
            UpdateVerticalLineRenderers();
        }

        // Public methods to control the visualizer position
        public void SetPosition(Vector3 position)
        {
            visualizerPosition = position;
            if (!updatePositionInRealtime)
            {
                UpdateContainerTransform();
            }
        }

        public void SetRotation(Vector3 rotation)
        {
            visualizerRotation = rotation;
            if (!updatePositionInRealtime)
            {
                UpdateContainerTransform();
            }
        }

        public void SetScale(Vector3 scale)
        {
            visualizerScale = scale;
            if (!updatePositionInRealtime)
            {
                UpdateContainerTransform();
            }
        }

        public void SetTransform(Vector3 position, Vector3 rotation, Vector3 scale)
        {
            visualizerPosition = position;
            visualizerRotation = rotation;
            visualizerScale = scale;
            if (!updatePositionInRealtime)
            {
                UpdateContainerTransform();
            }
        }

        public override void UpdateWaveform(List<float> samples)
        {
            if (gameObject.activeSelf)
            {
                UpdateVisualization(samples);
            }
        }

        /// <summary>
        /// Sets the volume level and changes the color of the audio visualization based on volume thresholds.
        /// </summary>
        /// <param name="volume">The volume level (0.0 to 1.0+)</param>
        /// <param name="smooth">Whether to smooth the color transition (true) or change immediately (false)</param>
        public override void SetVolume(float volume, bool smooth = true)
        {
            // Store the current volume
            currentVolume = volume;// * addEffectConfig.audioScaleVol;

            // Determine color based on volume thresholds
            Color newColor;

            //if (currentVolume < 0.66f)
            //{
            //    // Green for lower volumes
            //    //newColor = Color.green;
            //    newColor = new Color(0.86f, 0.08f, 0.24f); // crimson
            //}
            //else if (currentVolume < 0.76f)
            //{
            //    // Yellow for medium-low volumes
            //    //newColor = Color.yellow;
            //    newColor = new Color(0.86f, 0.08f, 0.24f); // crimson
            //}
            //else if (currentVolume < 0.88f)
            //{
            //    // Orange for medium-high volumes
            //    //newColor = new Color(1.0f, 0.5f, 0.0f); // Orange
            //    newColor = new Color(1f, 0.843f, 0f); // gold
            //}
            //else if (currentVolume < 1.0f)
            //{
            //    // Red for high volumes
            //    //newColor = Color.red;
            //    newColor = new Color(1f, 0.843f, 0f); // gold
            //}
            //else
            //{
            //    // Purple for very high volumes (above 1.0)
            //    //newColor = new Color(0.5f, 0.0f, 0.5f); // Purple
            //    newColor = new Color(0.2f, 1f, 0.2f); // green
            //}

            if (currentVolume <= 0.7f)
            {
                // 0 ~ 0.7 : 白 → 血紅
                float t = Mathf.InverseLerp(0f, 0.7f, currentVolume);
                newColor = Color.Lerp(white, crimson, t);
            }
            else if (currentVolume <= 0.9f)
            {
                // 0.7 ~ 0.9 : 血紅 → 金色
                float t = Mathf.InverseLerp(0.7f, 0.9f, currentVolume);
                newColor = Color.Lerp(crimson, gold, t);
            }
            else if (currentVolume <= 1.1f)
            {
                // 0.9 ~ 1.1 : 金色 → 綠色
                float t = Mathf.InverseLerp(0.9f, 1.1f, currentVolume);
                newColor = Color.Lerp(gold, green, t);
            }
            else if (currentVolume <= 1.3f)
            {
                // 0.9 ~ 1.1 : 綠色 → 紫色
                float t = Mathf.InverseLerp(1.1f, 1.3f, currentVolume);
                newColor = Color.Lerp(green, ceil, t);
            }
            else
            {
                // > 1.3 : CeilColor
                newColor = ceil;
            }

            if (smooth)
            {
                // Set the target color for smooth transition in Update()
                targetColor = newColor * brightness;  // 這裡乘上亮度

                // 保留原本透明度（避免 alpha 也被乘掉）
                targetColor.a = newColor.a;

                // Adjust color smooth speed based on how dramatic the change is
                colorSmoothSpeed = 5f;
            }
            else
            {
                // Immediately change colors
                targetColor = newColor;
                lineColor = newColor;
                glowColor = newColor;

                // Update all renderers immediately
                if (useGlowEffect)
                {
                    UpdateGlowEffect();
                }
                else
                {
                    // Update line renderer colors
                    if (topLineRenderer != null)
                    {
                        topLineRenderer.startColor = lineColor;
                        topLineRenderer.endColor = lineColor;
                    }

                    if (bottomLineRenderer != null)
                    {
                        bottomLineRenderer.startColor = lineColor;
                        bottomLineRenderer.endColor = lineColor;
                    }

                    // Update vertical line renderers
                    if (verticalLineRenderers != null)
                    {
                        foreach (LineRenderer lr in verticalLineRenderers)
                        {
                            if (lr != null)
                            {
                                lr.startColor = lineColor;
                                lr.endColor = lineColor;
                            }
                        }
                    }
                }
            }
        }
    }
}