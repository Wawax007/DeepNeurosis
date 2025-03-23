using UnityEngine;

public class DoorInteract : MonoBehaviour, IInteractable
{
    public float openAngle = 90f;
    public float openSpeed = 2f;
    public Transform pivot;
    private bool isOpen = false;
    private Quaternion initialRotation;
    private Quaternion targetRotation;

    void Start()
    {
        initialRotation = pivot.rotation;
        targetRotation = pivot.rotation * Quaternion.Euler(0, openAngle, 0);
    }

    public void Interact()
    {
        isOpen = !isOpen;
        StopAllCoroutines();
        StartCoroutine(RotateDoor());
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