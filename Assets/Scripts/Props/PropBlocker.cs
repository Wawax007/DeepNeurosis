using UnityEngine;

public class PropBlocker : MonoBehaviour
{
    public float radius = 1f;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}