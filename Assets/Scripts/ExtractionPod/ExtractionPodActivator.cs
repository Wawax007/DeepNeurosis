using System.Collections;
using UnityEngine;

/// <summary>
/// Active et orchestre la capsule d’extraction: activation interactable, transitions audio,
/// fondu écran et séquence de fin après validation de la console.
/// </summary>
public class ExtractionPodActivator : MonoBehaviour
{
    [Header("Interaction")]
    public Collider capsuleTrigger; // Le trigger qui détecte le joueur
    public MonoBehaviour interactionScript; // Optionnel : si tu as un script custom sur la capsule

    [Header("Audio")]
    public AudioClip extractionMusic;
    public AudioSource audioSource;

    [Header("Fade")]
    public CanvasGroup fadeCanvas; // CanvasGroup noir avec alpha 0 au départ
    public float fadeDuration = 2f;

    private bool capsuleEnabled = false;
    private bool hasBeenUsed = false;
    private bool musicStarted = false;
    [Header("Interaction Layer")]
    public string raycastLayerName = "Interact";
    public GameObject capsuleVisualRoot;
    public float transitionDuration = 1.5f;

    private void Start()
    {
        if (fadeCanvas != null)
            fadeCanvas.alpha = 0;
    }

    public void EnableCapsule()
    {
        capsuleEnabled = true;

        if (!string.IsNullOrEmpty(raycastLayerName) && capsuleVisualRoot != null)
        {
            int layer = LayerMask.NameToLayer(raycastLayerName);
            if (layer >= 0)
            {
                capsuleVisualRoot.layer = layer;

                foreach (Transform child in capsuleVisualRoot.transform)
                    child.gameObject.layer = layer;
            }
            else
            {
                Debug.LogWarning("[ExtractionPodActivator] Layer '" + raycastLayerName + "' introuvable !");
            }
        }

        Debug.Log("[ExtractionPodActivator] Capsule interactive !");
    }
    
    public void ApplyInteractionLayerIfConsoleValidated(bool isValidated)
    {
        if (!isValidated) return;

        EnableCapsule();
    }


    public void StartExtractionSequence()
    {
        if (hasBeenUsed) return;

        var controller = GameObject.FindObjectOfType<PlayerScripts.FirstPersonController>();
        if (controller != null) controller.enabled = false;

        var playerInput = GameObject.FindObjectOfType<UnityEngine.InputSystem.PlayerInput>();
        if (playerInput != null) playerInput.enabled = false;

        if (audioSource != null)
        {
            StartCoroutine(FadeOutAudioSpatialFX());
            audioSource.loop = false;
            audioSource.volume = 1f;
        }

        hasBeenUsed = true;
        StartCoroutine(HandleExtractionSequence());
    }


    private IEnumerator FadeOutAudioSpatialFX()
    {
        if (audioSource == null) yield break;

        var lowPass = audioSource.GetComponent<AudioLowPassFilter>();
        if (lowPass == null)
        {
            Debug.LogWarning("[ExtractionPodActivator] Aucun AudioLowPassFilter trouvé sur la capsule !");
            yield break;
        }

        lowPass.enabled = true;
        lowPass.cutoffFrequency = 22000f;

        float duration = transitionDuration * 1.5f;
        float t = 0f;

        Vector3 startPos = audioSource.transform.position;
        Vector3 targetPos = (GameObject.FindGameObjectWithTag("Player")?.transform.position ?? Vector3.zero) + Vector3.up * 1.5f;
        
        while (t < duration)
        {
            float ratio = Mathf.SmoothStep(0f, 1f, t / duration);

            if (audioSource.transform)
                audioSource.transform.position = Vector3.Lerp(startPos, targetPos, ratio);

            lowPass.cutoffFrequency = Mathf.Lerp(22000f, 800f, ratio);

            t += Time.deltaTime;
            yield return null;
        }

        audioSource.spatialBlend = 0f;
        audioSource.dopplerLevel = 0f;
        audioSource.bypassEffects = true;
        audioSource.bypassListenerEffects = true;
        audioSource.bypassReverbZones = true;
        audioSource.spatialize = false;
        audioSource.spatializePostEffects = false;

        // Et on enlève le filtre
        lowPass.enabled = false;

        Debug.Log("[ExtractionPodActivator] Transition avec LowPass fluide et propre.");
    }



    private void OnTriggerEnter(Collider other)
    {
        if (!capsuleEnabled || hasBeenUsed) return;

        if (other.CompareTag("Player"))
        {
            hasBeenUsed = true;
            StartCoroutine(HandleExtractionSequence());
        }
    }
    
    public void PlayExtractionMusic()
    {
        if (audioSource != null && extractionMusic != null && !musicStarted)
        {
            musicStarted = true;

            audioSource.clip = extractionMusic;
            audioSource.loop = true;

            // Effets activés
            audioSource.bypassEffects = false;
            audioSource.bypassListenerEffects = false;
            audioSource.bypassReverbZones = false;
            audioSource.spatialize = true;
            audioSource.spatializePostEffects = true;
            audioSource.spatialBlend = 1f;
            audioSource.dopplerLevel = 1f;
            audioSource.reverbZoneMix = 1f;

            audioSource.Play();
            Debug.Log("[ExtractionPodActivator] Musique d’extraction lancée avec effets.");
        }
    }


    private IEnumerator FadeOutAudioEffects()
    {
        float t = 0f;

        float startSpatialBlend = audioSource.spatialBlend;
        float startDoppler = audioSource.dopplerLevel;
        float startReverb = audioSource.reverbZoneMix;

        while (t < transitionDuration)
        {
            float ratio = t / transitionDuration;

            audioSource.spatialBlend = Mathf.Lerp(startSpatialBlend, 0f, ratio);
            audioSource.dopplerLevel = Mathf.Lerp(startDoppler, 0f, ratio);
            audioSource.reverbZoneMix = Mathf.Lerp(startReverb, 0f, ratio);

            t += Time.deltaTime;
            yield return null;
        }

        // Finaliser les paramètres
        audioSource.spatialBlend = 0f;
        audioSource.dopplerLevel = 0f;
        audioSource.reverbZoneMix = 0f;

        // Désactive les bypass
        audioSource.bypassEffects = true;
        audioSource.bypassListenerEffects = true;
        audioSource.bypassReverbZones = true;
        audioSource.spatialize = false;
        audioSource.spatializePostEffects = false;

        Debug.Log("[ExtractionPodActivator] Transition audio douce terminée.");
    }



    private IEnumerator HandleExtractionSequence()
    {
        Debug.Log("[ExtractionCapsule] Extraction déclenchée");

        if (fadeCanvas != null)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                fadeCanvas.alpha = Mathf.Lerp(0, 1, t / fadeDuration);
                t += Time.deltaTime;
                yield return null;
            }
            fadeCanvas.alpha = 1;
        }

        yield return new WaitForSeconds(2f);
        Debug.Log("[ExtractionCapsule] Fin du jeu atteinte !");
    }

}
