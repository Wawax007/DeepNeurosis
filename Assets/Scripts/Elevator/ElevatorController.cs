using UnityEngine;
using System.Collections;

/// <summary>
/// Contrôle le cycle complet d’un ascenseur (fermeture portes, déplacement, ding, ouverture).
/// </summary>
public class ElevatorController : MonoBehaviour
{
    [Header("Références")]
    /// <summary>
    /// Gestionnaire d’étages utilisé pour charger/switcher d’étage.
    /// </summary>
    public FloorManager floorManager;
    /// <summary>
    /// Animator contrôlant l’ouverture/fermeture des portes.
    /// </summary>
    public Animator doorAnimator;
    /// <summary>
    /// Source audio jouant les effets de l’ascenseur.
    /// </summary>
    public AudioSource audioSource;
    /// <summary>
    /// Bloqueur physique/logiciel empêchant l’entrée pendant le mouvement.
    /// </summary>
    public GameObject doorBlocker;

    [Header("Sons")]
    /// <summary>
    /// Son joué à la fermeture des portes.
    /// </summary>
    public AudioClip doorCloseClip;
    /// <summary>
    /// Son joué pendant le déplacement de la cabine.
    /// </summary>
    public AudioClip moveClip;
    /// <summary>
    /// Son joué à l’arrivée à l’étage (ding).
    /// </summary>
    public AudioClip dingClip;

    [Header("Timings")]
    /// <summary>
    /// Délai avant déplacement après fermeture des portes (secondes).
    /// </summary>
    public float doorCloseDelay = 5f;
    /// <summary>
    /// Durée approximative du déplacement (secondes).
    /// </summary>
    public float moveDuration = 8f;
    /// <summary>
    /// Durée d’ouverture des portes après l’arrivée (secondes).
    /// </summary>
    public float doorOpenDuration = 2f;

    private bool isMoving = false;

    /// <summary>
    /// Handle l’appui sur un bouton d’étage.
    /// </summary>
    /// <param name="floorIndex">Index de l’étage demandé.</param>
    public void OnFloorButtonPressed(int floorIndex)
    {
        if (isMoving) return; 
        Debug.Log($"[ElevatorController] Button pressed for floor {floorIndex}");
        StartCoroutine(HandleFloorTransition(floorIndex));
    }

    private IEnumerator HandleFloorTransition(int floorIndex)
    {
        isMoving = true;

        if (doorBlocker != null)
            doorBlocker.SetActive(true);

        // Ne déclenche l'anim que si un controller est présent
        if (doorAnimator != null && doorAnimator.runtimeAnimatorController != null)
            doorAnimator.SetTrigger("Close");

        if (doorCloseClip != null && audioSource != null)
            audioSource.PlayOneShot(doorCloseClip);

        yield return new WaitForSeconds(doorCloseDelay);

        if (moveClip != null && audioSource != null)
            audioSource.PlayOneShot(moveClip);

        floorManager.GoToFloor(floorIndex);

        yield return new WaitForSeconds(moveDuration);

        if (dingClip != null && audioSource != null)
            audioSource.PlayOneShot(dingClip);

        if (doorAnimator != null && doorAnimator.runtimeAnimatorController != null)
            doorAnimator.SetTrigger("Open");

        yield return new WaitForSeconds(doorOpenDuration); 

        if (doorBlocker != null)
            doorBlocker.SetActive(false);

        isMoving = false;
    }
}