// PlayerInteraction.cs
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float raycastRange = 3f;
    public LayerMask raycastLayer;  // Boutons, portes, etc. (les objets "interactables")

    [Header("Highlight")]
    private GameObject highlightedObject;
    private OutlineMeshCreator currentOutline;

    [Header("Pickup Physics")]
    public Transform holdPosition;   // Position devant le joueur où on tient l’objet
    public float holdSpring = 50f; 
    public float holdDamping = 20f;  
    public float maxHoldDistance = 4f;

    private Camera playerCamera;
    private Rigidbody heldObject;  // L'objet qu'on tient

    private void Start()
    {
        playerCamera = Camera.main;
    }

    private void Update()
    {
        // 1) Gérer le highlight en raycastant
        CheckHighlight();

        // 2) Si on tient un objet physique, on continue de le déplacer
        if (heldObject != null)
        {
            MoveHeldObject();
        }
    }

    // Méthode InputSystem (par exemple "OnInteract" mappé à E)
    public void OnInteract(InputValue value)
    {
        InteractOrPickup();
    }

    #region Highlight
    void CheckHighlight()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastRange, raycastLayer))
        {
            // Vérifie s’il y a un IInteractable
            IInteractable interactable = hit.collider.GetComponent<IInteractable>()
                ?? hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                // On récupère OutlineMeshCreator pour le surlignage
                OutlineMeshCreator outline = hit.collider.GetComponent<OutlineMeshCreator>()
                    ?? hit.collider.GetComponentInParent<OutlineMeshCreator>();

                // Si c'est un nouvel objet, on désactive l'ancien highlight
                if (hit.collider.gameObject != highlightedObject)
                {
                    DisableHighlight();

                    highlightedObject = hit.collider.gameObject;
                    currentOutline = outline;

                    if (currentOutline != null)
                        currentOutline.SetHighlight(true);
                }
                return;
            }
        }

        // Si pas d'IInteractable détecté, désactiver highlight
        DisableHighlight();
    }

    void DisableHighlight()
    {
        if (currentOutline != null)
        {
            currentOutline.SetHighlight(false);
        }
        highlightedObject = null;
        currentOutline = null;
    }
    #endregion

    #region Interact / Pickup
    void InteractOrPickup()
    {
        // Si on tient déjà un objet, on le lâche
        if (heldObject != null)
        {
            DropPhysicsProp();
            return;
        }

        // Sinon, on tente un raycast
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, raycastRange, raycastLayer))
        {
            // 1) Vérifie s’il y a un IInteractable
            IInteractable interactable = hit.collider.GetComponent<IInteractable>()
                ?? hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                // On déclenche l’interaction
                interactable.Interact();
                Debug.Log("Interacted with: " + hit.collider.name);
                return;
            }

            // 2) Sinon, check si c'est un Rigidbody (physique)
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                PickUpPhysicsProp(rb);
                return;
            }
        }

        Debug.Log("Rien à interagir ou ramasser.");
    }
    #endregion

    #region Pickup / Drop
    void PickUpPhysicsProp(Rigidbody rb)
    {
        heldObject = rb;
        heldObject.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        heldObject.isKinematic = false;  // On veut pouvoir l'agiter un peu
        heldObject.drag = 5f;
        heldObject.angularDrag = 5f;

        Debug.Log("Objet ramassé: " + rb.name);
    }

    void DropPhysicsProp()
    {
        if (heldObject == null) return;

        heldObject.drag = 0f;
        heldObject.angularDrag = 0.05f;
        heldObject.isKinematic = false;

        Debug.Log("Objet lâché: " + heldObject.name);
        heldObject = null;
    }

    void MoveHeldObject()
    {
        // On applique une force pour rapprocher l’objet du holdPosition
        Vector3 toHoldPos = holdPosition.position - heldObject.position;
        if (toHoldPos.magnitude > maxHoldDistance)
        {
            // Si l’objet s’éloigne trop, on le lâche
            DropPhysicsProp();
            return;
        }

        // On applique une force style "spring joint"
        heldObject.AddForce(toHoldPos * holdSpring, ForceMode.Acceleration);
        heldObject.AddForce(-heldObject.velocity * holdDamping, ForceMode.Acceleration);
    }
    #endregion
}
