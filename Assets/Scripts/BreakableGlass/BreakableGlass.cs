using UnityEngine;

public class BreakableGlass : MonoBehaviour
{
    public float breakForce = 5f; // Force minimale pour casser la vitre
    public GameObject brokenGlassPrefab; // Modèle de la vitre brisée
    public AudioClip breakSound; // Son de bris de verre

    private void OnCollisionEnter(Collision collision)
    {
        // Vérifier si l'objet a un Rigidbody
        Rigidbody rb = collision.rigidbody;
        if (rb != null)
        {
            float impactForce = collision.relativeVelocity.magnitude * rb.mass;
            if (impactForce >= breakForce)
            {
                BreakGlass();
            }
        }
    }

    private void BreakGlass()
    {
        if (brokenGlassPrefab)
        {
            Instantiate(brokenGlassPrefab, transform.position, Quaternion.Euler(0, 0, 0));
        }

        if (breakSound)
        {
            AudioSource.PlayClipAtPoint(breakSound, transform.position);
        }

        Destroy(gameObject); // Détruit la vitre originale
    }
}