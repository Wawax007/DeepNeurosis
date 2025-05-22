using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioLogPlayer : MonoBehaviour, IInteractable
{
    [Header("Audio Log Settings")]
    public AudioClip logClip;
    [TextArea] public string logSubtitle;
    public bool playOnStart = false;
    public bool playOnce = true;

    private AudioSource audioSource;
    private bool hasPlayed = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D audio
    }

    private void Start()
    {
        if (playOnStart)
        {
            PlayLog();
        }
    }

    public void PlayLog()
    {
        if (hasPlayed && playOnce) return;

        if (logClip != null)
        {
            audioSource.clip = logClip;
            audioSource.Play();
            hasPlayed = true;
        }
    }

    public void Interact()
    {
        PlayLog();
    }
}
