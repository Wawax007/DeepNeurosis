using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Suit les props d’énigme présents dans la cabine d’ascenseur afin de les préserver
/// lors des transitions d’étage, et notifie le transfert quand ils en sortent.
/// </summary>
public class ElevatorPropTracker : MonoBehaviour
{
    public HashSet<GameObject> trackedProps = new();
    public static ElevatorPropTracker Instance;

    private void Awake()
    {
        Instance = this;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ClueProps"))
        {
            trackedProps.Add(other.gameObject);
            Debug.Log($"[Elevator] Prop {other.name} entré.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (trackedProps.Contains(other.gameObject))
        {
            trackedProps.Remove(other.gameObject);
            Debug.Log($"[Elevator] Prop {other.name} sorti.");
            PropTransferManager.Instance.OnPropExitedElevator(other.gameObject);
        }
    }

    public bool IsInElevator(GameObject obj) => trackedProps.Contains(obj);
}
