using UnityEngine;

public class ElevatorPanel : MonoBehaviour
{
    public ElevatorManager elevatorManager;

    public void OnFloorButtonPressed(int floorNumber)
    {
        elevatorManager?.MoveToFloor(0);
    }
}