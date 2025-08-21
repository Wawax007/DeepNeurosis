using UnityEngine;
using PlayerScripts;

/// <summary>
/// Porte de casier permettant au joueur d’entrer/sortir d’une cachette et de masquer l’avatar.
/// </summary>
public class LockerDoor : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    public float openAngle = 142f; // Angle fermé
    public float closedAngle = 0f; // Angle ouvert
    public float rotationSpeed = 2f;
    public Transform hidePosition;

    private bool isOpen = false;
    private Quaternion openRotation;
    private Quaternion closedRotation;

    private void Start()
    {
        // Définir les rotations ouvertes et fermées
        openRotation = Quaternion.Euler(0, 0, openAngle);
        closedRotation = Quaternion.Euler(0, 0, closedAngle);
    }

    public void Interact()
    {
        isOpen = !isOpen; // Inverser l'état
        StopAllCoroutines();
        StartCoroutine(RotateDoor());
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && isOpen)
        {
            player.transform.position = hidePosition.position;
            player.GetComponent<FirstPersonController>().Hide(true);
            Debug.Log("Joueur caché dans le casier");
        }
        else
        {
            player.GetComponent<FirstPersonController>().Hide(false);
            Debug.Log("Joueur sorti du casier");
        }
    }
    
    private System.Collections.IEnumerator RotateDoor()
    {
        Quaternion targetRotation = isOpen ? openRotation : closedRotation;

        while (Quaternion.Angle(transform.localRotation, targetRotation) > 0.1f)
        {
            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * rotationSpeed);
            yield return null;
        }

        transform.localRotation = targetRotation; // Assurer une position finale précise
    }
}