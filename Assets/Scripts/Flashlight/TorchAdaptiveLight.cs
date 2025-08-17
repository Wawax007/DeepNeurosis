using UnityEngine;

public class TorchAdaptiveLight : MonoBehaviour
{
    [Header("Lights")]
    public Light outerLight; // Large angle, 50°
    public Light innerLight; // Faisceau serré, 34.7°

    [Header("Raycast Settings")]
    public float maxCheckDistance = 20f;
    public LayerMask detectionLayers;

    [Header("Range Settings")]
    public float minRange = 10f;     // Ton range actuel
    public float maxRange = 25f;

    [Header("Intensity Settings")]
    public float minIntensityOuter = 1f;
    public float maxIntensityOuter = 1.4f;

    public float minIntensityInner = 1.35f;
    public float maxIntensityInner = 2.0f;

    [Header("Smoothing")]
    public float smoothSpeed = 5f;

    private float targetRange;
    private float targetIntensityOuter;
    private float targetIntensityInner;

    void Update()
    {
        UpdateAdaptiveLighting();
    }

    void UpdateAdaptiveLighting()
    {
        // Raycast droit devant la torche
        Ray ray = new Ray(transform.position, transform.forward);
        float distance = maxCheckDistance;

        if (Physics.Raycast(ray, out RaycastHit hit, maxCheckDistance, detectionLayers))
        {
            distance = hit.distance;
        }

        // T = 0 → mur proche, T = 1 → ouvert
        float t = distance / maxCheckDistance;

        // Calculs de cibles
        targetRange = Mathf.Lerp(minRange, maxRange, t);
        targetIntensityOuter = Mathf.Lerp(minIntensityOuter, maxIntensityOuter, t);
        targetIntensityInner = Mathf.Lerp(minIntensityInner, maxIntensityInner, t);

        // Application lissée
        outerLight.range = Mathf.Lerp(outerLight.range, targetRange, Time.deltaTime * smoothSpeed);
        innerLight.range = Mathf.Lerp(innerLight.range, targetRange, Time.deltaTime * smoothSpeed);

        outerLight.intensity = Mathf.Lerp(outerLight.intensity, targetIntensityOuter, Time.deltaTime * smoothSpeed);
        innerLight.intensity = Mathf.Lerp(innerLight.intensity, targetIntensityInner, Time.deltaTime * smoothSpeed);
    }
}
