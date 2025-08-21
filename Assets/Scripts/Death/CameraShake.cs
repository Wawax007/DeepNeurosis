using UnityEngine;

/// <summary>
/// Service simple de secousse caméra avec intensité/durée, accessible via un singleton.
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;
    private Transform camTransform;
    private float shakeDuration;
    private float shakeMagnitude;
    private Vector3 originalPos;

    void Awake()
    {
        if (Instance == null)
            Instance = this;

        camTransform = Camera.main.transform;
        originalPos = camTransform.localPosition;
    }

    public void Shake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
        StopAllCoroutines();
        StartCoroutine(DoShake());
    }

    private System.Collections.IEnumerator DoShake()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            Vector3 randomPoint = originalPos + Random.insideUnitSphere * shakeMagnitude;
            camTransform.localPosition = new Vector3(randomPoint.x, randomPoint.y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        camTransform.localPosition = originalPos;
    }
}