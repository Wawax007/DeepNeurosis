using UnityEngine;

public class PhysicsProp : MonoBehaviour, IInteractable
{
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Interact()
    {
        Debug.Log("Attrapé : " + name);
        rb.isKinematic = false; // Simulation de prise en main
    }
}