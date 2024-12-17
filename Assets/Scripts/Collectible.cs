using UnityEngine;

public class Collectible : MonoBehaviour, IInteractable
{
    public string itemName;

    public void Interact()
    {
        Debug.Log("Collecté : " + itemName);
        gameObject.SetActive(false); // Simulation de collecte
    }
}