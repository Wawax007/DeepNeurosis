using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float raycastRange = 3f;            // Portée du Raycast pour attraper des objets physiques
    public LayerMask raycastLayer;             // Layer des objets physiques

    [Header("Sphere Interaction Settings")]
    public float interactionRadius = 2f;       // Rayon pour les interactions proches
    public LayerMask sphereLayer;              // Layer pour les objets interactifs à proximité

    [Header("Physics Prop Settings")]
    private Rigidbody heldObject;              // Objet actuellement tenu
    public Transform holdPosition;             // Position où tenir l'objet
    public float holdForce = 500f;             // Force pour attirer l'objet vers le point de maintien
    public float holdDamping = 10f;            // Amortissement pour réduire les oscillations

    private Camera playerCamera;

    private void Start()
    {
        playerCamera = Camera.main;
    }

    private void Update()
    {
        HandleSphereInteraction();   // Pour les objets interactifs proches
        HandleRaycastInteraction();  // Pour attraper les props physiques

        // Mise à jour de l'objet tenu avec une force physique
        if (heldObject != null)
            MoveHeldObject();
    }

    #region Sphere Interaction (Objets Proches)
    void HandleSphereInteraction()
    {
        Collider[] nearbyObjects = Physics.OverlapSphere(transform.position, interactionRadius, sphereLayer);

        foreach (Collider col in nearbyObjects)
        {
            IInteractable interactable = col.GetComponent<IInteractable>();
            if (interactable != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                interactable.Interact();
                Debug.Log("Interaction avec : " + col.name);
                return; // On interagit avec un seul objet
            }
        }
    }
    #endregion

    #region Raycast Interaction (Physics Props)
    void HandleRaycastInteraction()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (heldObject == null)
                TryPickUpPhysicsProp();
            else
                DropPhysicsProp();
        }
    }

    void TryPickUpPhysicsProp()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        // Dessiner le Raycast dans la scène
        Debug.DrawLine(ray.origin, ray.origin + ray.direction * raycastRange, Color.red, 1f);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastRange, raycastLayer))
        {
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                heldObject = rb;
                heldObject.drag = 10f;         // Augmenter le drag pour amortir le mouvement
                heldObject.angularDrag = 5f;   // Réduire la rotation brusque
                Debug.Log("Objet attrapé : " + rb.name);
            }
            else
            {
                Debug.LogWarning("Aucun Rigidbody trouvé sur l'objet touché.");
            }
        }
        else
        {
            Debug.Log("Raycast n'a touché aucun objet interactif.");
        }
    }

    void DropPhysicsProp()
    {
        heldObject.drag = 0f;         // Rétablir le drag par défaut
        heldObject.angularDrag = 0.05f;
        heldObject = null;            // Relâcher l'objet
        Debug.Log("Objet lâché.");
    }

    void MoveHeldObject()
    {
        if (heldObject == null) return;

        // Calcule la direction vers le point de maintien
        Vector3 targetPosition = holdPosition.position;
        Vector3 direction = targetPosition - heldObject.position;

        // Limite la force pour éviter les mouvements brusques
        float smoothForce = Mathf.Clamp(holdForce * direction.magnitude, 0, holdForce);

        // Déplace l'objet en douceur en utilisant MovePosition
        heldObject.MovePosition(Vector3.Lerp(heldObject.position, targetPosition, Time.deltaTime * holdDamping));
        
    }


    #endregion

    #region Debug Draw (Affiche les zones dans la scène)
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius); // Zone d'interaction
    }
    #endregion
}
