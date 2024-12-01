using UnityEngine;

public class FootstepManager : MonoBehaviour
{
    [Header("Footstep Settings")]
    public AudioClip[] footstepSounds;
    public AudioSource audioSource;
    public float walkStepRate = 0.5f;
    public float runStepRate = 0.3f;
    public float pitchVariation = 0.1f;
    public float minVolume = 0.10f;
    public float maxVolume = 0.25f;
    private float stepCooldown;
    

    public void PlayFootstep(bool isRunning)
    {
        if (stepCooldown > 0f)
        {
            stepCooldown -= Time.deltaTime;
            return;
        }

        stepCooldown = isRunning ? runStepRate : walkStepRate;

        // Jouer un son de pas aléatoire
        if (footstepSounds.Length > 0)
        {
            AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
            audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            audioSource.volume = Random.Range(minVolume, maxVolume);
            audioSource.PlayOneShot(clip);
        }
    }
}