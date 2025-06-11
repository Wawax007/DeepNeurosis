using System;
using System.Collections;
using PlayerScripts;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Random = UnityEngine.Random; // si tu utilises TextMeshPro

public class DeathManager : MonoBehaviour
{
    public FirstPersonController playerController;
    public Transform respawnPoint;
    public Image blackOverlay;
    public TextMeshProUGUI deathText;
    public AudioSource deathAudio;
    public AudioClip[] dreamWhisperClips;
    public AudioClip heartbeatClip;
    public float fadeDuration = 1f;
    public float respawnDelay = 1.5f;
    public bool isDying = false;
    private FloorManager floorManager;

    private string[] deathPhrases = new string[]
    {
        "You were never meant to leave.",
        "This isn’t over.",
        "He still watches.",
        "There is no surface.",
        "What did you think you’d find?"
    };

    public void Start()
    {
        floorManager = FindObjectOfType<FloorManager>();
    }

    public void TriggerDeath()
    {
        if (!isDying)
        {
            StartCoroutine(HandleDeath());
        }
    }

    private IEnumerator HandleDeath()
    {
        isDying = true;
        playerController.SetCameraLock(true);

        yield return StartCoroutine(FadeBlack(0f, 1f, fadeDuration));

        // Transition vers StartRoom si besoin
        if (floorManager != null && floorManager.currentFloor != -2)
        {
            floorManager.GoToFloor(-2);
            yield return new WaitForSeconds(1.5f);
        }

        // Sons
        if (deathAudio && dreamWhisperClips.Length > 0)
        {
            AudioClip randomClip = dreamWhisperClips[Random.Range(0, dreamWhisperClips.Length)];
            deathAudio.PlayOneShot(randomClip);
        }
        if (heartbeatClip)
            deathAudio.PlayOneShot(heartbeatClip, 0.5f);

        // Message cryptique
        string phrase = deathPhrases[Random.Range(0, deathPhrases.Length)];
        deathText.text = phrase;
        yield return StartCoroutine(FadeText(0f, 1f, 1f));
        
        yield return new WaitForSeconds(respawnDelay);

        // Téléportation sécurisée
        if (respawnPoint != null)
        {
            playerController.characterController.enabled = false;
            playerController.transform.position = respawnPoint.position;
            playerController.transform.rotation = respawnPoint.rotation;
            playerController.characterController.enabled = true;
        }
        else
        {
            Debug.LogWarning("[DeathManager] Aucun respawnPoint défini !");
        }

        yield return new WaitForSeconds(1f);
        
        // Fade out texte + overlay
        StartCoroutine(FadeText(1f, 0f, 1f));
        yield return StartCoroutine(FadeBlack(1f, 0f, fadeDuration));

        playerController.SetCameraLock(false);
        isDying = false;
    }


    private IEnumerator FadeBlack(float from, float to, float duration)
    {
        float t = 0f;
        Color color = blackOverlay.color;

        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, t / duration);
            blackOverlay.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }
        blackOverlay.color = new Color(color.r, color.g, color.b, to);
    }

    private IEnumerator FadeText(float from, float to, float duration)
    {
        float t = 0f;
        Color color = deathText.color;

        while (t < duration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, t / duration);
            deathText.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }
        deathText.color = new Color(color.r, color.g, color.b, to);
    }
}
