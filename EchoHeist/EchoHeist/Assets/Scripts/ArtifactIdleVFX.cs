using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ArtifactIdleVFX : MonoBehaviour
{
    [Header("Floating")]
    [SerializeField] private float bobHeight = 0.12f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Visual Pulse")]
    [SerializeField] private float scalePulseAmount = 0.05f;
    [SerializeField] private float scalePulseSpeed = 2.5f;

    [Header("Glow")]
    [SerializeField] private Transform glowTransform;
    [SerializeField] private float glowPulseAmount = 0.12f;
    [SerializeField] private float glowPulseSpeed = 2f;

    [SerializeField] private Light2D glowLight;
    [SerializeField] private float baseLightIntensity = 0.8f;
    [SerializeField] private float lightPulseAmount = 0.2f;
    [SerializeField] private float lightPulseSpeed = 2f;

    private Vector3 startLocalPosition;
    private Vector3 startLocalScale;
    private Vector3 glowStartScale;

    private void Start()
    {
        startLocalPosition = transform.localPosition;
        startLocalScale = transform.localScale;

        if (glowTransform != null)
        {
            glowStartScale = glowTransform.localScale;
        }
    }

    private void Update()
    {
        float time = Time.time;

        // Up-down floating
        float bobOffset = Mathf.Sin(time * bobSpeed) * bobHeight;
        transform.localPosition = startLocalPosition + new Vector3(0f, bobOffset, 0f);

        // Gentle scale pulse
        float scaleOffset = 1f + Mathf.Sin(time * scalePulseSpeed) * scalePulseAmount;
        transform.localScale = startLocalScale * scaleOffset;

        // Glow sprite pulse
        if (glowTransform != null)
        {
            float glowScale = 1f + Mathf.Sin(time * glowPulseSpeed) * glowPulseAmount;
            glowTransform.localScale = glowStartScale * glowScale;
        }

        // Light pulse
        if (glowLight != null)
        {
            glowLight.intensity = baseLightIntensity +
                                  Mathf.Sin(time * lightPulseSpeed) * lightPulseAmount;
        }
    }
}