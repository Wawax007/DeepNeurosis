using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Porte de comptoir interactive: s’ouvre/ferme, détecte et ancre un fusible,
/// puis allume une diode et ouvre l’ascenseur lorsqu’alimentée.
/// </summary>
public class CounterDoor : MonoBehaviour, IInteractable
{
    [Header("Porte")]
    public Transform pivot;
    public float openAngle = 143f;
    public float rotationSpeed = 2f;
    private bool isOpen = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;

    [Header("Fusible / Socket")]
    public Transform fuseSocket; // Point d'ancrage du fusible
    public float fuseDetectionRadius = 0.2f;
    public LayerMask physicsPropsLayer;
    private bool fuseInserted = false;

    [Header("Diode")]
    public Renderer diodeRenderer;
    public Material diodeOffMaterial;
    public Material diodeOnMaterial;
    public bool IsFuseInserted() => fuseInserted;

    private void Start()
    {
        closedRotation = pivot.localRotation;
        openRotation = Quaternion.Euler(pivot.localEulerAngles.x, pivot.localEulerAngles.y, openAngle);

        // Assure que la diode est rouge au départ
        if (diodeRenderer != null && diodeOffMaterial != null)
        {
            diodeRenderer.material = diodeOffMaterial;
        }
    }

    private void Update()
    {
        if (!fuseInserted && isOpen)
        {
            DetectAndInsertFuse();
        }
    }
    
    public void ForceInsertFuse()
    {
        fuseInserted = true;
        isOpen = true;

        if (diodeRenderer != null && diodeOnMaterial != null)
            diodeRenderer.material = diodeOnMaterial;

        // Assure l'ouverture des portes de l'ascenseur lors d'un chargement
        ElevatorController elevatorController = FindObjectOfType<ElevatorController>();
        if (elevatorController != null &&
            elevatorController.doorAnimator != null &&
            elevatorController.doorAnimator.runtimeAnimatorController != null)
        {
            elevatorController.doorAnimator.SetTrigger("Open");
        }

        FusibleItem fusible = FindObjectOfType<FusibleItem>();
        if (fusible != null && !fusible.IsAnchored)
        {
            fusible.AnchorTo(fuseSocket);
            fusible.transform.localScale = Vector3.one;
        }

        StopAllCoroutines();
        StartCoroutine(RotateDoor());
    }


    private void DetectAndInsertFuse()
    {
        Collider[] colliders = Physics.OverlapSphere(fuseSocket.position, fuseDetectionRadius, physicsPropsLayer);
        
        if (fuseInserted || !isOpen) return;

        foreach (Collider col in colliders)
        {
            Rigidbody rb = col.attachedRigidbody;
            FusibleItem fusible = col.GetComponent<FusibleItem>();
            if (fusible != null && !fusible.IsAnchored)
            {
                fusible.AnchorTo(fuseSocket);
                fusible.transform.localScale = Vector3.one;
                fuseInserted = true;

                if (diodeRenderer != null && diodeOnMaterial != null)
                {
                    diodeRenderer.material = diodeOnMaterial;
                    // Ouvre les portes de l’ascenseur
                    ElevatorController elevatorController = FindObjectOfType<ElevatorController>();
                    if (elevatorController != null && elevatorController.doorAnimator != null)
                    {
                        elevatorController.doorAnimator.SetTrigger("Open");
                    }
                }

                break;
            }
        }
    }


    public void Interact()
    {
        isOpen = !isOpen;
        StopAllCoroutines();
        StartCoroutine(RotateDoor());
    }

    private IEnumerator RotateDoor()
    {
        Quaternion targetRotation = isOpen ? openRotation : closedRotation;

        while (Quaternion.Angle(pivot.localRotation, targetRotation) > 0.1f)
        {
            pivot.localRotation = Quaternion.Lerp(pivot.localRotation, targetRotation, Time.deltaTime * rotationSpeed);
            yield return null;
        }

        pivot.localRotation = targetRotation;
    }

    private void OnDrawGizmosSelected()
    {
        if (fuseSocket != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(fuseSocket.position, fuseDetectionRadius);
        }
    }
}
