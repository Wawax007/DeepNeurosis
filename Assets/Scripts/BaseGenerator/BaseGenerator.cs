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
        // Pour éviter de générer dans l'eau (ou autre), on part de (200, 0) / roomSize
        Vector2 startPosition = new Vector2(200f / roomSize, 0f);
        Debug.Log($"Generating the first room at {startPosition}");

        // Instancie la première salle
        GameObject startRoom = InstantiateRoom(startPosition);
        RoomData startRoomData = new RoomData(startRoom, new bool[4]);
        roomPositions.Add(startPosition, startRoomData);

        // On stocke l'info des murs
        exportedRoomData.Add(new RoomExportData(startPosition, startRoomData.walls));

        // Instancie les autres salles
        for (int i = 1; i < numberOfRooms; i++)
        {
            Vector2 roomPosition;
            bool isPositionValid;

            do
            {
                roomPosition = GetRandomAdjacentPosition();
                isPositionValid = !roomPositions.ContainsKey(roomPosition);
            }
            while (!isPositionValid);

            GameObject newRoom = InstantiateRoom(roomPosition);
            roomPositions.Add(roomPosition, new RoomData(newRoom, new bool[4]));
            exportedRoomData.Add(new RoomExportData(roomPosition, new bool[4]));
        }

        // Ajuste quels murs sont externes/internes et gère la pose
        AdjustRoomWalls();
        CheckDoorErrors();
        ExportRoomData();
        yield break;
    }

    /// <summary>
    /// Détermine pour chaque salle :
    /// - quels murs sont "externes" (pas de salle voisine)
    /// - qui est "responsable" de placer le mur entre deux salles (porte ou non)
    /// </summary>
    void AdjustRoomWalls()
    {
        foreach (var entry in roomPositions)
        {
            Vector2 position = entry.Key;
            RoomData currentRoom = entry.Value;

            // Check salle voisine : s'il n'y en a pas dans une direction => mur externe
            bool isNorthExternal = !roomPositions.ContainsKey(position + Vector2.up);
            bool isSouthExternal = !roomPositions.ContainsKey(position + Vector2.down);
            bool isEastExternal = !roomPositions.ContainsKey(position + Vector2.right);
            bool isWestExternal = !roomPositions.ContainsKey(position + Vector2.left);

            // Pour export (walls[0]=North, [1]=South, [2]=East, [3]=West)
            currentRoom.walls[0] = isNorthExternal;
            currentRoom.walls[1] = isSouthExternal;
            currentRoom.walls[2] = isEastExternal;
            currentRoom.walls[3] = isWestExternal;

            // Indique si on doit mettre une "porte" dans telle direction
            bool northDoor = false;
            bool southDoor = false;
            bool eastDoor = false;
            bool westDoor = false;

            // Indique si on doit *placer* un mur (ou rien) à cet endroit
            bool placeNorthWall = true;
            bool placeSouthWall = true;
            bool placeEastWall = true;
            bool placeWestWall = true;

            // ----- Nord -----
            if (roomPositions.ContainsKey(position + Vector2.up))
            {
                // Si notre Y est plus petit que le voisin => c'est nous qui plaçons la porte
                if (position.y < (position + Vector2.up).y)
                {
                    northDoor = true; // On placera une porte
                }
                else
                {
                    // Le voisin a un Y plus petit, donc c'est lui qui place la porte/mur
                    placeNorthWall = false;
                }
            }
            // s'il n'y a pas de salle au Nord => mur externe => placeNorthWall = true (déjà true), mais pas de door

            // ----- Sud -----
            if (roomPositions.ContainsKey(position + Vector2.down))
            {
                if (position.y < (position + Vector2.down).y)
                {
                    southDoor = true;
                }
                else
                {
                    placeSouthWall = false;
                }
            }

            // ----- Est -----
            if (roomPositions.ContainsKey(position + Vector2.right))
            {
                if (position.x < (position + Vector2.right).x)
                {
                    eastDoor = true;
                }
                else
                {
                    placeEastWall = false;
                }
            }

            // ----- Ouest -----
            if (roomPositions.ContainsKey(position + Vector2.left))
            {
                if (position.x < (position + Vector2.left).x)
                {
                    westDoor = true;
                }
                else
                {
                    placeWestWall = false;
                }
            }

            // Lance la config (coroutine) de la salle
            RoomGenerator roomGen = currentRoom.roomObject.GetComponent<RoomGenerator>();
            StartCoroutine(roomGen.SetupRoomCoroutine(
                northDoor, southDoor, eastDoor, westDoor,
                isNorthExternal, isSouthExternal, isEastExternal, isWestExternal,
                placeNorthWall, placeSouthWall, placeEastWall, placeWestWall
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
        // Contrôle "naïf" : si une salle dit "pas de mur" alors que l'autre dit "mur", on peut détecter un mismatch
        foreach (var roomData in exportedRoomData)
        {
            Vector2 position = roomData.position;
            bool[] walls = roomData.walls;

            // Nord
            if (roomPositions.ContainsKey(position + Vector2.up))
            {
                bool northWall = walls[0];
                bool southWall = roomPositions[position + Vector2.up].walls[1];

                if (northWall != southWall)
                {
                    Debug.LogWarning($"Door mismatch between {position} and {position + Vector2.up}.");
                }
            }
            // Sud
            if (roomPositions.ContainsKey(position + Vector2.down))
            {
                bool southWall = walls[1];
                bool northWall = roomPositions[position + Vector2.down].walls[0];

                if (southWall != northWall)
                {
                    Debug.LogWarning($"Door mismatch between {position} and {position + Vector2.down}.");
                }
            }
            // Est
            if (roomPositions.ContainsKey(position + Vector2.right))
            {
                bool eastWall = walls[2];
                bool westWall = roomPositions[position + Vector2.right].walls[3];

                if (eastWall != westWall)
                {
                    Debug.LogWarning($"Door mismatch between {position} and {position + Vector2.right}.");
                }
            }
            // Ouest
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
