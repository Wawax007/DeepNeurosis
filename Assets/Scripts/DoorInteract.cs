using UnityEngine;

public class DoorInteract : MonoBehaviour, IInteractable
{
    public float openAngle = 90f;
    public float openSpeed = 2f;

    private bool isOpen = false;
    private Quaternion initialRotation;
    private Quaternion targetRotation;

    void Start()
    {
        initialRotation = transform.rotation;
        targetRotation = Quaternion.Euler(transform.eulerAngles + Vector3.up * openAngle);
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
        while (Quaternion.Angle(transform.rotation, target) > 0.1f)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, target, Time.deltaTime * openSpeed);
            yield return null;
        }
    }
}