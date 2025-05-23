using UnityEngine;

public class BreakableGlass : MonoBehaviour
{
    public float breakForce = 5f; // Force minimale pour casser la vitre
    public GameObject brokenGlassPrefab; // Modèle de la vitre brisée
    public AudioClip breakSound; // Son de bris de verre
    public float fragmentImpulseForce = 2.5f; // Force très douce et ciblée

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb != null)
        {
            float impactForce = collision.relativeVelocity.magnitude * rb.mass;
            if (impactForce >= breakForce)
            {
                Vector3 impactPoint = collision.contacts[0].point;
                Vector3 impactDirection = collision.relativeVelocity.normalized;
                BreakGlass(impactPoint, impactDirection);
            }
        }
    }

    private void BreakGlass(Vector3 impactPoint, Vector3 impactDirection)
    {
        Transform parentTransform = GameObject.Find("startRoom")?.transform;

        if (brokenGlassPrefab)
        {
            GameObject broken = Instantiate(
                brokenGlassPrefab,
                transform.position,
                Quaternion.Euler(0, 90, 0),
                parentTransform
            );

            foreach (var rb in broken.GetComponentsInChildren<Rigidbody>())
            {
                if (rb == null) continue;
                rb.isKinematic = false;

                Vector3 toFragment = rb.transform.position - impactPoint;
                float distance = toFragment.magnitude;

                // Courbe d’atténuation rapide avec la distance (plus > exponentielle)
                float forceMultiplier = Mathf.Clamp01(1f - distance / 0.5f); // 0.5m rayon efficace
                forceMultiplier = Mathf.Pow(forceMultiplier, 2f); // accentue la décroissance

                // Force avec variation
                float force = Mathf.Lerp(2f, 6f, forceMultiplier); // morceaux proches = +6

                // Direction principale + légère variation
                Vector3 dir = (impactDirection + Random.insideUnitSphere * 0.05f).normalized;

                rb.AddForce(dir * force, ForceMode.Impulse);
            }
        }

        if (breakSound)
        {
            AudioSource.PlayClipAtPoint(breakSound, transform.position);
        }

        Destroy(gameObject);
    }

}
