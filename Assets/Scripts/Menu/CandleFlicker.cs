using UnityEngine;

public class CandleFlicker : MonoBehaviour
{
    public Light candleLight;
    public float baseRange = 8.75f;
    public float baseIntensity = 0.80f;
    public float flickerSpeed = 0.1f; // Temps entre chaque cible de flicker
    public float rangeVariation = 0.5f;
    public float intensityVariation = 0.2f;

    private float targetIntensity; // Prochaine valeur cible pour l'intensité
    private float targetRange; // Prochaine valeur cible pour le range
    private float flickerTimer;

    void Start()
    {
        if (candleLight == null)
        {
            candleLight = GetComponent<Light>();
        }

        // Initialisation des cibles
        targetIntensity = baseIntensity;
        targetRange = baseRange;
    }

    void Update()
    {
        // Timer pour déterminer la prochaine cible
        flickerTimer -= Time.deltaTime;

        if (flickerTimer <= 0f)
        {
            // Définir de nouvelles cibles pour l'intensité et le range
            targetIntensity = baseIntensity + Random.Range(-intensityVariation, intensityVariation);
            targetRange = baseRange + Random.Range(-rangeVariation, rangeVariation);

            // Réinitialiser le timer
            flickerTimer = flickerSpeed;
        }

        // Lissage vers les valeurs cibles
        candleLight.intensity = Mathf.Lerp(candleLight.intensity, targetIntensity, Time.deltaTime * 5f);
        candleLight.range = Mathf.Lerp(candleLight.range, targetRange, Time.deltaTime * 5f);
    }
}