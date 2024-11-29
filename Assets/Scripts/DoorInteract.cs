using UnityEngine;

public class DoorInteract : MonoBehaviour
{
    [Header("Interaction Settings")]
    public Transform doorTransform; // Transform de la porte (pivot)
    public float openAngle = 90f; // Angle d'ouverture de la porte
    public float openSpeed = 2f; // Vitesse d'ouverture
    public string interactKey = "e"; // Touche d'interaction (par défaut "E")

    [Header("Proximity Settings")]
    public float interactionDistance = 3f; // Distance à laquelle le joueur peut interagir
    public GameObject interactionPromptPrefab; // Préfab pour l'indication visuelle d'interaction

    private Transform playerTransform; // Transform du joueur (trouvé dynamiquement)
    private GameObject interactionPrompt; // Instance de l'indication visuelle
    private bool isDoorOpen = false;
    private bool isPlayerNearby = false;
    private Quaternion initialRotation; // Rotation initiale de la porte
    private Quaternion targetRotation; // Rotation cible de la porte

    void Start()
    {
        // Configuration de la porte
        initialRotation = doorTransform.rotation;
        targetRotation = Quaternion.Euler(doorTransform.eulerAngles + Vector3.up * openAngle);

        // Trouver dynamiquement le joueur (par tag)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("Aucun joueur trouvé avec le tag 'Player'. Assurez-vous que votre joueur a ce tag !");
        }

        // Instancier ou désactiver l'indication d'interaction
        if (interactionPromptPrefab != null)
        {
            interactionPrompt = Instantiate(interactionPromptPrefab, transform.position + Vector3.up * 2f, Quaternion.identity, transform);
            interactionPrompt.SetActive(false); // Masquer par défaut
        }
        else
        {
            Debug.LogWarning("Aucun prefab d'indication d'interaction assigné !");
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        CheckPlayerProximity();

        if (isPlayerNearby && Input.GetKeyDown(interactKey))
        {
            ToggleDoor(); // Ouvre ou ferme la porte
        }

        UpdateDoorRotation();
    }

    private void CheckPlayerProximity()
    {
        float distance = Vector3.Distance(playerTransform.position, transform.position);
        isPlayerNearby = distance <= interactionDistance;

        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(isPlayerNearby); // Affiche ou masque le prompt
        }
    }

    private void ToggleDoor()
    {
        isDoorOpen = !isDoorOpen; // Alterne entre ouvert et fermé
    }

    private void UpdateDoorRotation()
    {
        // Interpolation pour ouvrir ou fermer la porte
        doorTransform.rotation = Quaternion.Lerp(
            doorTransform.rotation,
            isDoorOpen ? targetRotation : initialRotation,
            Time.deltaTime * openSpeed
        );
    }
}
