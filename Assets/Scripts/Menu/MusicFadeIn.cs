using UnityEngine;

public class MusicFadeIn : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    public float fadeDuration = 5f;

    private void Start()
    {
        audioSource.volume = 0.02f;
        StartCoroutine(DelayedStart());
    }

    private System.Collections.IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(1f);
        audioSource.Play();
        StartCoroutine(FadeIn());
    }

    private System.Collections.IEnumerator FadeIn()
    {
        float targetVolume = 0.8f;
        float currentTime = 0f;

        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0, targetVolume, currentTime / fadeDuration);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }
}