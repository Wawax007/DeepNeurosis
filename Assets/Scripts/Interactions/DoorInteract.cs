using UnityEngine;

/// <summary>
/// Gère l’ouverture/fermeture d’une porte via interaction joueur.
/// </summary>
public class DoorInteract : MonoBehaviour, IInteractable
{
    [Header("Door Settings")]
    /// <summary>
    /// Angle d’ouverture de la porte (en degrés).
    /// </summary>
    public float openAngle = 90f;
    /// <summary>
    /// Vitesse d’animation de l’ouverture/fermeture.
    /// </summary>
    public float openSpeed = 2f;

    [Header("References")]
    /// <summary>
    /// Point de pivot utilisé pour la rotation de la porte.
    /// </summary>
    public Transform pivot;
    /// <summary>
    /// Obstacle NavMesh pour bloquer le pathfinding quand la porte est fermée.
    /// </summary>
    public UnityEngine.AI.NavMeshObstacle obstacle;

    private bool isOpen = false;
    private Quaternion initialRotation;
    private Quaternion targetRotation;

    /// <summary>
    /// Indique si la porte est actuellement ouverte.
    /// </summary>
    public bool IsOpen => isOpen;

    private void Start()
    {
        InitializeRotations();
    }

    /// <summary>
    /// Déclenche l’interaction d’ouverture/fermeture de la porte.
    /// </summary>
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