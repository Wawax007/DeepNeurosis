using UnityEngine;
using System.Collections;

/// <summary>
/// Bouton physique qui implémente IInteractable (un simple exemple d'interface).
/// Quand on "Interact" avec, il s'enfonce brièvement et appelle l'ascenseur.
/// </summary>
public class PhysicalFloorButton : MonoBehaviour, IInteractable
{
    [Header("Floor Button Settings")]
    public int floorNumber = 0;
    public float pushDepth = 0.01f;

    [Header("References")]
    public ElevatorController elevatorController;

    private Vector3 originalPos;

    private void Start()
    {
        originalPos = transform.localPosition;
    }

    public void Interact()
    {
        Debug.Log("[PhysicalFloorButton] Bouton appuyé - étage " + floorNumber);

        // Lance l’animation du bouton qui s’enfonce
        StartCoroutine(PressAnimation());

        // Appelle la méthode de l'ascenseur pour aller à l’étage correspondant
        if (elevatorController != null)
        {
            elevatorController.OnFloorButtonPressed(floorNumber);
        }
        else
        {
            Debug.LogWarning("[PhysicalFloorButton] Pas d'ElevatorController assigné !");
        }
    }

    private IEnumerator PressAnimation()
    {
        // Enfonce un peu le bouton
        transform.localPosition = originalPos - transform.forward * pushDepth;
        yield return new WaitForSeconds(0.1f);

        // Restaure la position originale
        transform.localPosition = originalPos;
    }
}