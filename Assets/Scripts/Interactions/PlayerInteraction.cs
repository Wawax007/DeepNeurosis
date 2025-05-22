// PlayerInteraction.cs

using ExtractionConsole.Module;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

    [Header("UI")]
    public Image interactionIcon;

    [Header("Hold Control")]
    public float scrollSpeed = 0.5f;
    public float minHoldDistance = 1f;
    public float maxHoldDistanceLimit = 5f;

    private float currentHoldDistance;
    private bool isRotatingObject = false;
    private PlayerScripts.FirstPersonController playerController;

    
    
    private void Start()
    {
        playerCamera = Camera.main;
        currentHoldDistance = Vector3.Distance(playerCamera.transform.position, holdPosition.position);
        playerController = FindObjectOfType<PlayerScripts.FirstPersonController>();
    }

    private void Update()
    {
        // 1) Gérer le highlight en raycastant
        CheckHighlight();
        HandleZoomInput();
        HandleObjectRotationInput();
    }
    
    private void FixedUpdate()
    {
        if (heldObject != null)
        {
            if (IsObjectAnchored(heldObject))
            {
                Debug.Log("Objet ancré détecté → libération forcée");
                heldObject = null;
                return;
            }

            MoveHeldObject();
        }
    }

    void HandleZoomInput()
    {
        if (heldObject == null) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentHoldDistance = Mathf.Clamp(currentHoldDistance + scroll * scrollSpeed * Time.deltaTime, minHoldDistance, maxHoldDistanceLimit);
            holdPosition.localPosition = new Vector3(0f, 0f, currentHoldDistance);
        }
    }

    void HandleObjectRotationInput()
    {
        if (heldObject == null)
        {
            if (isRotatingObject)
            {
                isRotatingObject = false;
                playerController.SetCameraLock(false);
            }
            return;
        }

        bool rKeyHeld = Keyboard.current.rKey.isPressed;

        if (rKeyHeld && !isRotatingObject)
        {
            isRotatingObject = true;
            playerController.SetCameraLock(true); // Lock caméra quand rotation commence
        }
        else if (!rKeyHeld && isRotatingObject)
        {
            isRotatingObject = false;
            playerController.SetCameraLock(false); // Unlock caméra quand rotation termine
        }

        if (isRotatingObject)
        {
            float mouseX = Mouse.current.delta.x.ReadValue();
            float mouseY = Mouse.current.delta.y.ReadValue();
            Vector3 rotation = new Vector3(-mouseY, -mouseX, 0f);
            heldObject.transform.Rotate(rotation, Space.Self);
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
        // Si un objet est tenu, on n'affiche pas d'icône ni de highlight
        if (heldObject != null)
        {
            DisableHighlight();
            if (interactionIcon != null && interactionIcon.enabled)
                interactionIcon.enabled = false;
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastRange, raycastLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>()
                                         ?? hit.collider.GetComponentInParent<IInteractable>();

            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();

            if (interactable != null || rb != null)
            {
                if (interactable != null)
                {
                    OutlineMeshCreator outline = hit.collider.GetComponent<OutlineMeshCreator>()
                                                 ?? hit.collider.GetComponentInParent<OutlineMeshCreator>();

                    if (hit.collider.gameObject != highlightedObject)
                    {
                        DisableHighlight();
                        highlightedObject = hit.collider.gameObject;
                        currentOutline = outline;
                        if (currentOutline != null)
                            currentOutline.SetHighlight(true);
                    }
                }

                if (interactionIcon != null && !interactionIcon.enabled)
                    interactionIcon.enabled = true;

                return;
            }
        }

        DisableHighlight();
        if (interactionIcon != null && interactionIcon.enabled)
            interactionIcon.enabled = false;
    }

    /// <summary>

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
                interactable.Interact();
                Debug.Log("Interacted with: " + hit.collider.name);
                return;
            }
            
            ValidateButton validateButton = hit.collider.GetComponent<ValidateButton>()
                                            ?? hit.collider.GetComponentInParent<ValidateButton>();
            if (validateButton != null)
            {
                validateButton.Press();
                return;
            }

            RotarySelector selector = hit.collider.GetComponent<RotarySelector>()
                                      ?? hit.collider.GetComponentInParent<RotarySelector>();
            if (selector != null)
            {
                selector.Rotate();
                Debug.Log("RotarySelector tourné: " + selector.name);
                return;
            }

            // 3) Sinon, check si c'est un Rigidbody (physique)
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
        if (IsObjectAnchored(rb)) return;

        heldObject = rb;
        ConfigureHeldObjectPhysics(heldObject);

        Debug.Log("Objet ramassé: " + rb.name);
    }

    bool IsObjectAnchored(Rigidbody rb)
    {
        var fusible = rb.GetComponent<FusibleItem>();
        if (fusible != null && fusible.IsAnchored)
        {
            Debug.Log("Ce fusible est déjà inséré.");
            return true;
        }

        var moduleItem = rb.GetComponent<ModuleItem>();
        if (moduleItem != null && moduleItem.IsAnchored)
        {
            Debug.Log("Cet objet est ancré dans un socket. Impossible de le ramasser.");
            return true;
        }

        return false;
    }

    void ConfigureHeldObjectPhysics(Rigidbody rb)
    {
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.isKinematic = false;
        rb.drag = 5f;
        rb.angularDrag = 5f;
    }

    void DropPhysicsProp()
    {
        if (heldObject == null) return;

        if (IsObjectAnchored(heldObject))
        {
            Debug.Log("Lâcher annulé. L'objet est ancré ou inséré.");
            heldObject = null;
            return;
        }

        ResetObjectPhysics(heldObject);

        Debug.Log("Objet lâché: " + heldObject.name);
        heldObject = null;
    }

    void ResetObjectPhysics(Rigidbody obj)
    {
        obj.drag = 0f;
        obj.angularDrag = 0.05f;
        obj.isKinematic = false;
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
