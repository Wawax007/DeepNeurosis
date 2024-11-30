using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;

public class BaseGenerator : MonoBehaviour
{
    public GameObject roomPrefab;
    public int numberOfRooms = 5;
    public float roomSize = 50f;

    private Dictionary<Vector2, RoomData> roomPositions = new Dictionary<Vector2, RoomData>();
    private List<RoomExportData> exportedRoomData = new List<RoomExportData>();

    void Start()
    {
        Debug.Log("Starting base generation...");
        StartCoroutine(GenerateMap());
    }

    IEnumerator GenerateMap()
    {
        // Modifier la position de départ à (200, 0) pour eviter le volume underwater
        Vector2 startPosition = new Vector2(200f / roomSize, 0f); // Diviser par roomSize pour correspondre à l'unité logique
        Debug.Log($"Generating the first room at {startPosition}");

        GameObject startRoom = InstantiateRoom(startPosition);
        RoomData startRoomData = new RoomData(startRoom, new bool[4]);
        roomPositions.Add(startPosition, startRoomData);

        exportedRoomData.Add(new RoomExportData(startPosition, startRoomData.walls));

        for (int i = 1; i < numberOfRooms; i++)
        {
            Vector2 roomPosition;
            bool isPositionValid;

            do
            {
                roomPosition = GetRandomAdjacentPosition();
                isPositionValid = !roomPositions.ContainsKey(roomPosition);
            } while (!isPositionValid);

            GameObject newRoom = InstantiateRoom(roomPosition);
            roomPositions.Add(roomPosition, new RoomData(newRoom, new bool[4]));

            exportedRoomData.Add(new RoomExportData(roomPosition, new bool[4]));
        }

        AdjustRoomWalls();
        CheckDoorErrors();
        ExportRoomData();
        yield break;
    }

    void AdjustRoomWalls()
    {
        foreach (var entry in roomPositions)
        {
            Vector2 position = entry.Key;
            RoomData currentRoom = entry.Value;

            bool isNorthExternal = !roomPositions.ContainsKey(position + Vector2.up);
            bool isSouthExternal = !roomPositions.ContainsKey(position + Vector2.down);
            bool isEastExternal = !roomPositions.ContainsKey(position + Vector2.right);
            bool isWestExternal = !roomPositions.ContainsKey(position + Vector2.left);

            currentRoom.walls[0] = isNorthExternal;
            currentRoom.walls[1] = isSouthExternal;
            currentRoom.walls[2] = isEastExternal;
            currentRoom.walls[3] = isWestExternal;

            RoomGenerator roomGen = currentRoom.roomObject.GetComponent<RoomGenerator>();
            StartCoroutine(roomGen.SetupRoomCoroutine(
                !currentRoom.walls[0], 
                !currentRoom.walls[1], 
                !currentRoom.walls[2], 
                !currentRoom.walls[3],
                isNorthExternal,
                isSouthExternal,
                isEastExternal,
                isWestExternal
            ));
        }
    }

    GameObject InstantiateRoom(Vector2 roomPosition)
    {
        Vector3 worldPosition = new Vector3(roomPosition.x * roomSize, 0, roomPosition.y * roomSize);
        return Instantiate(roomPrefab, worldPosition, Quaternion.identity);
    }

    Vector2 GetRandomAdjacentPosition()
    {
        List<Vector2> directions = new List<Vector2> { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        Vector2 randomDirection = directions[Random.Range(0, directions.Count)];
        Vector2 existingRoomPosition = new List<Vector2>(roomPositions.Keys)[Random.Range(0, roomPositions.Count)];
        return existingRoomPosition + randomDirection;
    }

    void CheckDoorErrors()
    {
        foreach (var roomData in exportedRoomData)
        {
            Vector2 position = roomData.position;
            bool[] walls = roomData.walls;

            if (roomPositions.ContainsKey(position + Vector2.up))
            {
                bool northWall = walls[0];
                bool southWall = roomPositions[position + Vector2.up].walls[1];

                if (northWall != southWall)
                {
                    Debug.LogWarning($"Door mismatch between {position} and {position + Vector2.up}.");
                }
            }

            if (roomPositions.ContainsKey(position + Vector2.down))
            {
                bool southWall = walls[1];
                bool northWall = roomPositions[position + Vector2.down].walls[0];

                if (southWall != northWall)
                {
                    Debug.LogWarning($"Door mismatch between {position} and {position + Vector2.down}.");
                }
            }

            if (roomPositions.ContainsKey(position + Vector2.right))
            {
                bool eastWall = walls[2];
                bool westWall = roomPositions[position + Vector2.right].walls[3];

                if (eastWall != westWall)
                {
                    Debug.LogWarning($"Door mismatch between {position} and {position + Vector2.right}.");
                }
            }

            if (roomPositions.ContainsKey(position + Vector2.left))
            {
                bool westWall = walls[3];
                bool eastWall = roomPositions[position + Vector2.left].walls[2];

                if (westWall != eastWall)
                {
                    Debug.LogWarning($"Door mismatch between {position} and {position + Vector2.left}.");
                }
            }
        }
    }

    void ExportRoomData()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var room in exportedRoomData)
        {
            sb.AppendLine($"Position: {room.position}, Walls: [{string.Join(",", room.walls)}]");
        }

        string filePath = Path.Combine(Application.persistentDataPath, "RoomData.txt");
        File.WriteAllText(filePath, sb.ToString());
        Debug.Log($"Room data exported to {filePath}");
    }
}

public class RoomExportData
{
    public Vector2 position;
    public bool[] walls;

    public RoomExportData(Vector2 position, bool[] walls)
    {
        this.position = position;
        this.walls = walls;
    }
}

public class RoomData
{
    public GameObject roomObject;
    public bool[] walls;

    public RoomData(GameObject roomObject, bool[] walls)
    {
        this.roomObject = roomObject;
        this.walls = walls;
    }
}
