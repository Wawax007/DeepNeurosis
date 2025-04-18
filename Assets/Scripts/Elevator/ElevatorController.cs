using UnityEngine;

public class ElevatorController : MonoBehaviour
{
    public FloorManager floorManager;

    // Appelé par un UnityEvent ou un "onClick" d'un bouton
    public void OnFloorButtonPressed(int floorIndex)
    {
        Debug.Log($"[ElevatorController] Button pressed for floor {floorIndex}");
        floorManager.GoToFloor(floorIndex);
    }
}