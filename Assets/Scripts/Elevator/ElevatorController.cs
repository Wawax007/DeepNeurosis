using UnityEngine;
using System.Collections;

public class ElevatorController : MonoBehaviour
{
    [Header("Références")]
    public FloorManager floorManager;
    public Animator doorAnimator;
    public AudioSource audioSource;
    public GameObject doorBlocker;

    [Header("Sons")]
    public AudioClip doorCloseClip;
    public AudioClip moveClip;
    public AudioClip dingClip;

    [Header("Timings")]
    public float doorCloseDelay = 5f;
    public float moveDuration = 8f;
    public float doorOpenDuration = 2f;

    private bool isMoving = false;

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

        if (doorAnimator != null)
            doorAnimator.SetTrigger("Close");

        if (doorCloseClip != null)
            audioSource.PlayOneShot(doorCloseClip);

        yield return new WaitForSeconds(doorCloseDelay);

        if (moveClip != null)
            audioSource.PlayOneShot(moveClip);

        floorManager.GoToFloor(floorIndex);

        yield return new WaitForSeconds(moveDuration);

        if (dingClip != null)
            audioSource.PlayOneShot(dingClip);

        if (doorAnimator != null)
            doorAnimator.SetTrigger("Open");

        yield return new WaitForSeconds(doorOpenDuration); 

        if (doorBlocker != null)
            doorBlocker.SetActive(false);

        isMoving = false;
    }
}