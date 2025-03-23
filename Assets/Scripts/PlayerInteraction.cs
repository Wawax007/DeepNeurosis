using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float raycastRange = 3f;      // Portée du Raycast pour attraper des objets physiques
    public LayerMask raycastLayer;       // Layer des objets physiques

    [Header("Sphere Interaction Settings")]
    public float interactionRadius = 2f; // Rayon pour les interactions proches
    public LayerMask sphereLayer;        // Layer des objets interactifs à proximité

    [Header("Physics Prop Settings")]
    private Rigidbody heldObject;        // Objet actuellement tenu
    public Transform holdPosition;       // Position où tenir l'objet

    [Tooltip("Force pour rapprocher l'objet (effet 'spring').")]
    public float holdSpring = 50f;

    [Tooltip("Damping pour réduire la vitesse de l'objet (effet 'frein').")]
    public float holdDamping = 20f;

    [Tooltip("Distance max avant de lâcher l'objet (optionnel).")]
    public float maxHoldDistance = 4f;

    private Camera playerCamera;

    private void Start()
    {
        playerCamera = Camera.main;
    }

    private void Update()
    {
        // Gestion des interactions (sphere + raycast) en Update
        HandleSphereInteraction();
        HandleRaycastInteraction();
    }

    private void FixedUpdate()
    {
        // Mise à jour de l'objet tenu en physique, à chaque FixedUpdate
        if (heldObject != null)
        {
            MoveHeldObject();
        }
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
                return; // Interagit avec un seul objet par pression
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
            {
                TryPickUpPhysicsProp();
            }
            else
            {
                DropPhysicsProp();
            }
        }
    }

    void TryPickUpPhysicsProp()
    {
        // Lancer un ray en face de la caméra pour détecter un Rigidbody
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        Debug.DrawLine(ray.origin, ray.origin + ray.direction * raycastRange, Color.red, 1f);

        if (Physics.Raycast(ray, out RaycastHit hit, raycastRange, raycastLayer))
        {
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Assigner l'objet tenu
                heldObject = rb;

                // Activer la physique en continu pour éviter que l'objet traverse
                heldObject.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                // Rendre l'objet complètement physique (pas kinematic)
                heldObject.isKinematic = false;

                // Augmenter la friction et la rotation pour éviter trop d'oscillations
                heldObject.drag = 5f;
                heldObject.angularDrag = 5f;

                // Facultatif: Ajuster la masse si besoin
                // heldObject.mass = 2f;

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
        if (heldObject == null) return;

        // Rétablir les valeurs par défaut
        heldObject.drag = 0f;
        heldObject.angularDrag = 0.05f;
        heldObject.isKinematic = false;

        // Retirer la référence
        heldObject = null;
        Debug.Log("Objet lâché.");
    }

    /// <summary>
    /// Déplace l'objet tenu vers holdPosition en appliquant une force physique (spring/damping).
    /// </summary>
    void MoveHeldObject()
    {
        if (heldObject == null) return;

        // Calcul de la direction vers la position de maintien
        Vector3 toHoldPos = holdPosition.position - heldObject.position;

        // Optionnel : si trop loin, on lâche pour éviter de le tirer à travers un mur
        if (toHoldPos.magnitude > maxHoldDistance)
        {
            DropPhysicsProp();
            return;
        }

        // "Spring": Applique une force qui attire l'objet vers holdPosition
        // ForceMode.Acceleration pour ignorer la masse
        heldObject.AddForce(toHoldPos * holdSpring, ForceMode.Acceleration);

        // "Damping": Applique une force opposée à la vitesse pour éviter l'accélération infinie
        heldObject.AddForce(-heldObject.velocity * holdDamping, ForceMode.Acceleration);
    }
    #endregion

    #region Debug Draw (Affiche les zones dans la scène)
    private void OnDrawGizmos()
    {
        // Permet de visualiser la sphère d'interaction dans la scène
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
    #endregion
}
