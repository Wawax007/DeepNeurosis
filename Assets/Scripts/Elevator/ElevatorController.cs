using UnityEngine;
using System.Collections;

public class ElevatorController : MonoBehaviour
{
    [Header("Références")]
    public FloorManager floorManager;
    public Animator doorAnimator; // Animator avec "Open" et "Close"
    public AudioSource audioSource;

    [Header("Sons")]
    public AudioClip doorCloseClip;
    public AudioClip moveClip;
    public AudioClip dingClip;

    [Header("Timings")]
    public float doorCloseDelay = 5;
    public float moveDuration = 8f;

    private bool isMoving = false;

    // Appelé par un UnityEvent ou un bouton UI
    public void OnFloorButtonPressed(int floorIndex)
    {
        if (isMoving) return; // Empêche les spams
        Debug.Log($"[ElevatorController] Button pressed for floor {floorIndex}");
        StartCoroutine(HandleFloorTransition(floorIndex));
    }

    private IEnumerator HandleFloorTransition(int floorIndex)
    {
        isMoving = true;

        // 1. Fermer les portes
        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger("Close");
        }

        if (doorCloseClip != null)
        {
            audioSource.PlayOneShot(doorCloseClip);
        }

        yield return new WaitForSeconds(doorCloseDelay);

        // 2. Jouer le son de mouvement
        if (moveClip != null)
        {
            audioSource.PlayOneShot(moveClip);
        }

        // 3. Lancer le changement d'étage pendant le mouvement
        floorManager.GoToFloor(floorIndex);

        // 4. Attente pendant que l'étage charge
        yield return new WaitForSeconds(moveDuration);

        // 5. Ding de fin
        if (dingClip != null)
        {
            audioSource.PlayOneShot(dingClip);
        }

        // 6. Ouvrir les portes
        if (doorAnimator != null)
        {
            doorAnimator.SetTrigger("Open");
        }

        isMoving = false;
    }
}