using UnityEngine;

/// <summary>
/// Objet physique basique attrapable: active la physique et journalise l’action à l’interaction.
/// </summary>
public class PhysicsProp : MonoBehaviour, IInteractable
{
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }


    public void Interact()
    {
        Debug.Log("Attrapé : " + name);
        rb.isKinematic = false;
    }
}