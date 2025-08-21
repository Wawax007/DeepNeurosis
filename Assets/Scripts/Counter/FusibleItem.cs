using UnityEngine;

/// <summary>
/// Élément « fusible » insérable: s’ancre dans un socket en désactivant sa physique,
/// se parentant au socket et devenant immobile.
/// </summary>
public class FusibleItem : MonoBehaviour
{
    public bool IsAnchored { get; private set; }

    public void AnchorTo(Transform targetSocket)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.Sleep();
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        transform.position = targetSocket.position;
        transform.rotation = targetSocket.rotation;
        transform.SetParent(targetSocket);

        IsAnchored = true;
    }
}