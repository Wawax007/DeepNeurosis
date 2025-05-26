using UnityEngine;

public class DoorInteract : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    public float openAngle = 90f;
    public float openSpeed = 2f;

    [Header("References")]
    public Transform pivot;
    public UnityEngine.AI.NavMeshObstacle obstacle;

    private bool isOpen = false;
    private Quaternion initialRotation;
    private Quaternion targetRotation;
    public bool IsOpen => isOpen;

    private void Start()
    {
        InitializeRotations();
    }

    public void Interact()
    {
        ToggleDoorState();
        StopAllCoroutines();
        StartCoroutine(RotateDoor());
    }

    private void InitializeRotations()
    {
        initialRotation = pivot.rotation;
        targetRotation = pivot.rotation * Quaternion.Euler(0, openAngle, 0);
    }

    private void ToggleDoorState()
    {
        isOpen = !isOpen;
        if (isOpen)
        {
            OpenDoor();
        }
        else
        {
            CloseDoor();
        }
    }

    private void OpenDoor()
    {
        obstacle.carving = false;
    }

    private void CloseDoor()
    {
        obstacle.carving = true;
    }

    private System.Collections.IEnumerator RotateDoor()
    {
        Quaternion target = isOpen ? targetRotation : initialRotation;
        while (Quaternion.Angle(pivot.rotation, target) > 0.1f)
        {
            pivot.rotation = Quaternion.Slerp(pivot.rotation, target, openSpeed * Time.deltaTime);
            yield return null;
        }
    }
}