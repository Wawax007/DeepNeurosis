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

    // Liste pour stocker les informations des salles
    private List<RoomExportData> exportedRoomData = new List<RoomExportData>();

    void Start()
    {
        Debug.Log("Starting base generation...");
        StartCoroutine(GenerateMap());
    }

    IEnumerator GenerateMap()
    {
        Vector2 startPosition = Vector2.zero;
        Debug.Log($"Generating the first room at {startPosition}");

        // Création de la première salle
        GameObject startRoom = InstantiateRoom(startPosition);
        RoomData startRoomData = new RoomData(startRoom, new bool[4]); // Par défaut, tous les murs à false
        roomPositions.Add(startPosition, startRoomData);

        exportedRoomData.Add(new RoomExportData(startPosition, startRoomData.walls));

        for (int i = 1; i < numberOfRooms; i++)
        {
            Vector2 roomPosition;
            bool isPositionValid;

            do
            {
                roomPosition = GetRandomAdjacentPosition();
                Debug.Log($"Trying to generate room #{i + 1} at position {roomPosition}");
                isPositionValid = !roomPositions.ContainsKey(roomPosition);

                if (!isPositionValid)
                    Debug.Log($"Position {roomPosition} is already occupied. Searching for a new position...");

            } while (!isPositionValid);

            GameObject newRoom = InstantiateRoom(roomPosition);
            Debug.Log($"Room #{i + 1} instantiated at {roomPosition}");

            roomPositions.Add(roomPosition, new RoomData(newRoom, new bool[4])); // Murs à false par défaut

            exportedRoomData.Add(new RoomExportData(roomPosition, new bool[4]));
        }

        // Une fois que toutes les salles sont créées, nous ajustons les murs
        AdjustRoomWalls();

        // Après avoir ajusté les murs, on vérifie les incohérences de portes
        CheckDoorErrors();

        // Export des données de position des salles
        ExportRoomData();
        yield break;
    }

    
    
    void AdjustRoomWalls()
    {
        foreach (var entry in roomPositions)
        {
            Vector2 position = entry.Key;
            RoomData currentRoom = entry.Value;

            // Ajustement des murs
            // Nord
            if (roomPositions.ContainsKey(position + Vector2.up))
            {
                currentRoom.walls[0] = false; // Pas de mur au nord, porte à la place
                roomPositions[position + Vector2.up].walls[1] = false; // Synchronisation avec le sud de la salle adjacente
            }
            else
            {
                currentRoom.walls[0] = true; // Mur plein si aucune salle au nord
            }

            // Sud
            if (roomPositions.ContainsKey(position + Vector2.down))
            {
                currentRoom.walls[1] = false; // Pas de mur au sud, porte à la place
                roomPositions[position + Vector2.down].walls[0] = false; // Synchronisation avec le nord de la salle adjacente
            }
            else
            {
                currentRoom.walls[1] = true; // Mur plein si aucune salle au sud
            }

            // Est
            if (roomPositions.ContainsKey(position + Vector2.right))
            {
                currentRoom.walls[2] = false; // Pas de mur à l'est, porte à la place
                roomPositions[position + Vector2.right].walls[3] = false; // Synchronisation avec l'ouest de la salle adjacente
            }
            else
            {
                currentRoom.walls[2] = true; // Mur plein si aucune salle à l'est
            }

            // Ouest
            if (roomPositions.ContainsKey(position + Vector2.left))
            {
                currentRoom.walls[3] = false; // Pas de mur à l'ouest, porte à la place
                roomPositions[position + Vector2.left].walls[2] = false; // Synchronisation avec l'est de la salle adjacente
            }
            else
            {
                currentRoom.walls[3] = true; // Mur plein si aucune salle à l'ouest
            }

            // Instancier les murs/portes après l'ajustement
            RoomGenerator roomGen = currentRoom.roomObject.GetComponent<RoomGenerator>();
            StartCoroutine(roomGen.SetupRoomCoroutine(
                !currentRoom.walls[0], // Si pas de mur au nord, mettre une porte
                !currentRoom.walls[1], // Si pas de mur au sud, mettre une porte
                !currentRoom.walls[2], // Si pas de mur à l'est, mettre une porte
                !currentRoom.walls[3]  // Si pas de mur à l'ouest, mettre une porte
            ));
        }
    }



    GameObject InstantiateRoom(Vector2 roomPosition)
    {
        Vector3 worldPosition = new Vector3(roomPosition.x * roomSize, 0, roomPosition.y * roomSize);
        Debug.Log($"Instantiating room at world position {worldPosition}");
        return Instantiate(roomPrefab, worldPosition, Quaternion.identity);
    }

    Vector2 GetRandomAdjacentPosition()
    {
        List<Vector2> directions = new List<Vector2> { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
        Vector2 randomDirection = directions[Random.Range(0, directions.Count)];
        Vector2 existingRoomPosition = new List<Vector2>(roomPositions.Keys)[Random.Range(0, roomPositions.Count)];

        Debug.Log($"Generated adjacent position from {existingRoomPosition}, direction {randomDirection}");
        return existingRoomPosition + randomDirection;
    }

    bool[] DetermineWalls(Vector2 position)
    {
        bool[] walls = new bool[4]; // North, South, East, West

        // Check for neighboring rooms and share walls accordingly
        if (roomPositions.ContainsKey(position + Vector2.up)) // North neighbor
        {
            walls[0] = roomPositions[position + Vector2.up].walls[1]; // North shares South wall of the room above
        }
        else
        {
            walls[0] = true; // Set true if no neighbor, meaning a wall is placed
        }

        if (roomPositions.ContainsKey(position + Vector2.down)) // South neighbor
        {
            walls[1] = roomPositions[position + Vector2.down].walls[0]; // South shares North wall of the room below
        }
        else
        {
            walls[1] = true; // Set true if no neighbor
        }

        if (roomPositions.ContainsKey(position + Vector2.right)) // East neighbor
        {
            walls[2] = roomPositions[position + Vector2.right].walls[3]; // East shares West wall of the room to the right
        }
        else
        {
            walls[2] = true; // Set true if no neighbor
        }

        if (roomPositions.ContainsKey(position + Vector2.left)) // West neighbor
        {
            walls[3] = roomPositions[position + Vector2.left].walls[2]; // West shares East wall of the room to the left
        }
        else
        {
            walls[3] = true; // Set true if no neighbor
        }

        return walls;
    }


    // Vérification des erreurs de portes
    void CheckDoorErrors()
    {
        foreach (var roomData in exportedRoomData)
        {
            Vector2 position = roomData.position;
            bool[] walls = roomData.walls;

            // Vérifier les connexions de portes avec les salles adjacentes
            if (roomPositions.ContainsKey(position + Vector2.up))
            {
                bool northWall = walls[0];
                bool southWall = roomPositions[position + Vector2.up].walls[1];

                if (northWall != southWall)
                {
                    Debug.LogWarning($"Door mismatch between room at {position} (North) and room at {position + Vector2.up} (South).");
                }
            }

            if (roomPositions.ContainsKey(position + Vector2.down))
            {
                bool southWall = walls[1];
                bool northWall = roomPositions[position + Vector2.down].walls[0];

                if (southWall != northWall)
                {
                    Debug.LogWarning($"Door mismatch between room at {position} (South) and room at {position + Vector2.down} (North).");
                }
            }

            if (roomPositions.ContainsKey(position + Vector2.right))
            {
                bool eastWall = walls[2];
                bool westWall = roomPositions[position + Vector2.right].walls[3];

                if (eastWall != westWall)
                {
                    Debug.LogWarning($"Door mismatch between room at {position} (East) and room at {position + Vector2.right} (West).");
                }
            }

            if (roomPositions.ContainsKey(position + Vector2.left))
            {
                bool westWall = walls[3];
                bool eastWall = roomPositions[position + Vector2.left].walls[2];

                if (westWall != eastWall)
                {
                    Debug.LogWarning($"Door mismatch between room at {position} (West) and room at {position + Vector2.left} (East).");
                }
            }
        }
    }

    // Export des données de position des salles dans un fichier texte
    void ExportRoomData()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var room in exportedRoomData)
        {
            sb.AppendLine($"Position: {room.position}, Walls: [{string.Join(",", room.walls)}]");
        }

        // Enregistrer dans un fichier texte
        string filePath = Path.Combine(Application.persistentDataPath, "RoomData.txt");
        File.WriteAllText(filePath, sb.ToString());
        Debug.Log($"Room data exported to {filePath}");
    }

    IEnumerator UpdateAdjacentRooms(Vector2 roomPosition)
    {
        Debug.Log($"Updating adjacent rooms at {roomPosition}...");
        yield return new WaitForSeconds(0.1f);

        RoomGenerator currentRoom = roomPositions[roomPosition].roomObject.GetComponent<RoomGenerator>();

        // Mises à jour des salles adjacentes
        if (roomPositions.ContainsKey(roomPosition + Vector2.up))
        {
            RoomGenerator adjacentRoom = roomPositions[roomPosition + Vector2.up].roomObject.GetComponent<RoomGenerator>();
            yield return StartCoroutine(SynchronizeRooms(currentRoom, adjacentRoom, 0, 1));
        }

        if (roomPositions.ContainsKey(roomPosition + Vector2.down))
        {
            RoomGenerator adjacentRoom = roomPositions[roomPosition + Vector2.down].roomObject.GetComponent<RoomGenerator>();
            yield return StartCoroutine(SynchronizeRooms(currentRoom, adjacentRoom, 1, 0));
        }

        if (roomPositions.ContainsKey(roomPosition + Vector2.right))
        {
            RoomGenerator adjacentRoom = roomPositions[roomPosition + Vector2.right].roomObject.GetComponent<RoomGenerator>();
            yield return StartCoroutine(SynchronizeRooms(currentRoom, adjacentRoom, 2, 3));
        }

        if (roomPositions.ContainsKey(roomPosition + Vector2.left))
        {
            RoomGenerator adjacentRoom = roomPositions[roomPosition + Vector2.left].roomObject.GetComponent<RoomGenerator>();
            yield return StartCoroutine(SynchronizeRooms(currentRoom, adjacentRoom, 3, 2));
        }
    }

    IEnumerator SynchronizeRooms(RoomGenerator currentRoom, RoomGenerator adjacentRoom, int currentRoomWall, int adjacentRoomWall)
    {
        // Synchroniser les murs entre deux salles adjacentes
        if (!currentRoom.IsWallInstantiated(currentRoomWall) && !adjacentRoom.IsWallInstantiated(adjacentRoomWall))
        {
            yield return StartCoroutine(currentRoom.SetupRoomCoroutine(
                currentRoomWall == 0, currentRoomWall == 1, currentRoomWall == 2, currentRoomWall == 3));
            yield return StartCoroutine(adjacentRoom.SetupRoomCoroutine(
                adjacentRoomWall == 0, adjacentRoomWall == 1, adjacentRoomWall == 2, adjacentRoomWall == 3));
        }
    }
    
    Vector2 GetDirection(int index)
    {
        switch (index)
        {
            case 0: return Vector2.up;
            case 1: return Vector2.down;
            case 2: return Vector2.right;
            case 3: return Vector2.left;
            default: return Vector2.zero;
        }
    }
}


// Structure pour exporter les données des salles
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
    public bool[] walls; // [North, South, East, West]

    public RoomData(GameObject roomObject, bool[] walls)
    {
        this.roomObject = roomObject;
        this.walls = walls;
    }
}