using System.Collections;
using UnityEngine;

public class ElevatorManager : MonoBehaviour
{
    [Header("Ascenseur")]
    public Animator elevatorAnimator;
    public Transform generationPoint; // Position devant les portes de l'ascenseur
    public GameObject baseGeneratorPrefab; // Prefab contenant BaseGenerator + roomPrefab assigné

    [Header("Effets")]
    public AudioSource elevatorAudio; // Bruit de déplacement (optionnel)
    public float fakeTravelDuration = 3f;

    private GameObject currentFloorInstance;
    private bool isTransitioning = false;
    private int currentFloor = 0;

    public void MoveToFloor(int floorIndex)
    {
        if (isTransitioning || floorIndex == currentFloor) return;
        StartCoroutine(TransitionToFloor(floorIndex));
    }

    private IEnumerator TransitionToFloor(int targetFloor)
    {
        isTransitioning = true;

        // Fermer les portes
        if (elevatorAnimator != null)
            elevatorAnimator.SetTrigger("CloseDoors");

        yield return new WaitForSeconds(1.5f); // Animation de fermeture

        // Lancer le bruitage ou ambiance de déplacement
        if (elevatorAudio != null)
            elevatorAudio.Play();

        // Détruire l’étage précédent
        if (currentFloorInstance != null)
            Destroy(currentFloorInstance);

        // Attente pour simuler le trajet
        yield return new WaitForSeconds(fakeTravelDuration);

        // Générer le nouvel étage devant les portes
        currentFloorInstance = Instantiate(baseGeneratorPrefab, generationPoint.position, Quaternion.identity);

        BaseGenerator generator = currentFloorInstance.GetComponent<BaseGenerator>();
        if (generator != null)
        {
            generator.numberOfRooms = 5 + targetFloor * 2;
            generator.roomSize = 50f;
        }

        // Arrêter le son
        if (elevatorAudio != null)
            elevatorAudio.Stop();

        // Ouvrir les portes
        if (elevatorAnimator != null)
            elevatorAnimator.SetTrigger("OpenDoors");

        currentFloor = targetFloor;
        isTransitioning = false;
    }
}
