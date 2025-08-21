using UnityEngine;

/// <summary>
/// Objet collectable basique: se désactive à l’interaction et journalise son nom.
/// </summary>
public class Collectible : MonoBehaviour, IInteractable
{
    public string itemName;

    public void Interact()
    {
        Debug.Log("Collecté : " + itemName);
        gameObject.SetActive(false); // Simulation de collecte
    }
}