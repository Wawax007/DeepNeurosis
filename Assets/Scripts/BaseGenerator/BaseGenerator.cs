using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gère la génération procédurale d'un "étage" (un ensemble de salles).
/// </summary>
public class BaseGenerator : MonoBehaviour
{
    [Header("Prefabs & Settings")]
    public GameObject roomPrefab;

    [Min(1)]
    public int numberOfRooms = 5;

    [Min(1f)]
    public float roomSize = 50f;

    // Le point d’ancrage où est placé l’ascenseur
    public Vector3 floorAnchor = new Vector3(183.021194f, 0f, -4.9f);

    // Données internes de l’étage
    private readonly Dictionary<Vector2, RoomData> roomPositions = new Dictionary<Vector2, RoomData>();
    private readonly List<RoomExportData> exportedRoomData = new List<RoomExportData>();

    public bool IsFloorReady { get; set; }

    #region Public API
    /// <summary>
    /// Génère un nouvel étage "propre" de façon procédurale.
    /// </summary>
    public void GenerateFloor(int floorIndex)
    {
        ClearOldData();
        Debug.Log($"[BaseGenerator] Generating floor {floorIndex} …");
        StartCoroutine(GenerateMapCoroutine(floorIndex));
    }

    /// <summary>
    /// Construit un étage depuis des données sauvegardées.
    /// </summary>
    public void LoadFloorFromData(FloorSaveData floorData)
    {
        ClearOldData();
        Debug.Log($"[BaseGenerator] Loading floor from JSON data (floorIndex: {floorData.floorIndex})");

        foreach (var roomInfo in floorData.rooms)
        {
            Vector2 pos      = roomInfo.position;
            GameObject room  = InstantiateRoom(pos);

            // Ré‑applique le seed aux générateurs internes
            var partGen = room.GetComponent<InternalPartitionGenerator>();
            if (partGen != null) partGen.SetSeed(roomInfo.partitionSeed);

            // On partage la même instance du tableau walls entre les deux structures
            bool[] walls = (bool[])roomInfo.walls.Clone();
            roomPositions.Add(pos, new RoomData(room, walls));
            exportedRoomData.Add(new RoomExportData(pos, walls, roomInfo.partitionSeed));
        }

        // Ajuste murs/portes au cas où le format sauvegardé est ancien
        AdjustRoomWalls();
        CheckDoorErrors();

        IsFloorReady = true;
    }

    /// <summary>
    /// Prépare les infos pour la sauvegarde JSON
    /// </summary>
    public FloorSaveData GetFloorData(int floorIndex)
    {
        // S'assure que les murs dans exportedRoomData sont à jour
        foreach (var export in exportedRoomData)
        {
            if (roomPositions.TryGetValue(export.position, out RoomData data))
            {
                data.walls.CopyTo(export.walls, 0);
            }
        }

        return new FloorSaveData
        {
            floorIndex = floorIndex,
            rooms      = new List<RoomExportData>(exportedRoomData)
        };
    }
    #endregion

    #region Generation
    IEnumerator GenerateMapCoroutine(int floorIndex)
    {
        // 1. Crée la salle de départ (0,0)
        Vector2 startGridPos = Vector2.zero;
        Debug.Log($"Generating first room at grid {startGridPos}");

        GameObject startRoom = InstantiateRoom(startGridPos);
        int startSeed = Random.Range(int.MinValue, int.MaxValue);

        var startPartGen = startRoom.GetComponent<InternalPartitionGenerator>();
        if (startPartGen != null) startPartGen.SetSeed(startSeed);

        bool[] startWalls = new bool[4];
        roomPositions.Add(startGridPos, new RoomData(startRoom, startWalls));
        exportedRoomData.Add(new RoomExportData(startGridPos, startWalls, startSeed));
        
        switch (floorIndex)
        {
            case -1:
                numberOfRooms = 2; // Étages -1 : 2 salles
                break;
            case 0:
                numberOfRooms = 3; // Étages 0 : 3 salles
                break;
            case 1:
                numberOfRooms = 2; // Étages 1 : 2 salles
                break;
            default:
                numberOfRooms = Mathf.Clamp(numberOfRooms, 1, 100); // Par défaut, limite entre 1 et 100
                break;
        }
        
        // 2. Crée les autres salles
        for (int i = 1; i < numberOfRooms; i++)
        {
            Vector2 roomPos;
            do
            {
                roomPos = GetRandomAdjacentPosition();
            } while (roomPositions.ContainsKey(roomPos));

            GameObject newRoom = InstantiateRoom(roomPos);
            int roomSeed = Random.Range(int.MinValue, int.MaxValue);

            var partGen = newRoom.GetComponent<InternalPartitionGenerator>();
            if (partGen != null) partGen.SetSeed(roomSeed);

            bool[] walls = new bool[4];
            roomPositions.Add(roomPos, new RoomData(newRoom, walls));
            exportedRoomData.Add(new RoomExportData(roomPos, walls, roomSeed));
        }

        // 3. Fin de génération – ajuste tout
        AdjustRoomWalls();
        CheckDoorErrors();

        IsFloorReady = true;
        yield break;
    }
    #endregion

    #region Room utilities
    /// <summary>
    /// Calcule les murs/portes internes/externes
    /// </summary>
    void AdjustRoomWalls()
    {
        foreach (var entry in roomPositions)
        {
            Vector2 pos      = entry.Key;
            RoomData room    = entry.Value;
            bool[] walls     = room.walls;

            // Présence de voisins = pas de mur
            bool northExt = !roomPositions.ContainsKey(pos + Vector2.up);
            bool southExt = !roomPositions.ContainsKey(pos + Vector2.down);
            bool eastExt  = !roomPositions.ContainsKey(pos + Vector2.right);
            bool westExt  = !roomPositions.ContainsKey(pos + Vector2.left);

            walls[0] = northExt; // N
            walls[1] = southExt; // S
            walls[2] = eastExt;  // E
            walls[3] = westExt;  // W

            bool northDoor = false, southDoor = false, eastDoor = false, westDoor = false;
            bool placeN = true, placeS = true, placeE = true, placeW = true;

            // Nord
            if (!northExt)
            {
                if (pos.y < (pos + Vector2.up).y) northDoor = true;
                else placeN = false;
            }
            // Sud
            if (!southExt)
            {
                if (pos.y < (pos + Vector2.down).y) southDoor = true;
                else placeS = false;
            }
            // Est
            if (!eastExt)
            {
                if (pos.x < (pos + Vector2.right).x) eastDoor = true;
                else placeE = false;
            }
            // Ouest
            if (!westExt)
            {
                if (pos.x < (pos + Vector2.left).x) westDoor = true;
                else placeW = false;
            }

            // La salle (0,0) est connectée à l'ascenseur au sud
            if (pos == Vector2.zero)
            {
                placeS           = false;
                southDoor        = false;
                walls[1]         = false;
            }

            RoomGenerator roomGen = room.roomObject.GetComponent<RoomGenerator>();
            if (roomGen != null)
            {
                StartCoroutine(roomGen.SetupRoomCoroutine(
                    northDoor, southDoor, eastDoor, westDoor,
                    northExt, southExt, eastExt, westExt,
                    placeN, placeS, placeE, placeW
                ));
            }
        }
    }

    /// <summary>
    /// Instancie une salle selon sa position "grille" relative.
    /// </summary>
    private GameObject InstantiateRoom(Vector2 gridPos)
    {
        float half = roomSize * 0.5f;
        Vector3 offset = new Vector3(
            gridPos.x * roomSize,
            0f,
            (gridPos.y * roomSize) + half   // On ajoute half sur Z pour que le bord Sud colle à l'ascenseur
        );

        Vector3 worldPos = floorAnchor + offset;
        return Instantiate(roomPrefab, worldPos, Quaternion.identity, transform);
    }

    /// <summary>
    /// Trouve au hasard une position voisine d'une salle existante
    /// (on retire Vector2.down pour éviter de construire derrière l'ascenseur).
    /// </summary>
    Vector2 GetRandomAdjacentPosition()
    {
        List<Vector2> directions = new List<Vector2>
        {
            Vector2.up,
            Vector2.left,
            Vector2.right
        };

        Vector2 randomDir   = directions[Random.Range(0, directions.Count)];
        Vector2 existingPos = new List<Vector2>(roomPositions.Keys)[Random.Range(0, roomPositions.Count)];
        return existingPos + randomDir;
    }

    /// <summary>
    /// Vérification d'éventuels conflits de murs
    /// </summary>
    void CheckDoorErrors()
    {
        foreach (var export in exportedRoomData)
        {
            Vector2 position = export.position;
            bool[]  walls    = export.walls;

            // Nord
            if (roomPositions.ContainsKey(position + Vector2.up))
            {
                bool northWall = walls[0];
                bool southWall = roomPositions[position + Vector2.up].walls[1];
                if (northWall != southWall)
                    Debug.LogWarning($"Door mismatch between {position} and {position + Vector2.up}.");
            }
            // Sud
            if (roomPositions.ContainsKey(position + Vector2.down))
            {
                bool southWall = walls[1];
                bool northWall = roomPositions[position + Vector2.down].walls[0];
                if (southWall != northWall)
                    Debug.LogWarning($"Door mismatch between {position} and {position + Vector2.down}.");
            }
            // Est
            if (roomPositions.ContainsKey(position + Vector2.right))
            {
                bool eastWall = walls[2];
                bool westWall = roomPositions[position + Vector2.right].walls[3];
                if (eastWall != westWall)
                    Debug.LogWarning($"Door mismatch between {position} and {position + Vector2.right}.");
            }
            // Ouest
            if (roomPositions.ContainsKey(position + Vector2.left))
            {
                bool westWall = walls[3];
                bool eastWall = roomPositions[position + Vector2.left].walls[2];
                if (westWall != eastWall)
                    Debug.LogWarning($"Door mismatch between {position} and {position + Vector2.left}.");
            }
        }
    }
    #endregion

    #region Housekeeping
    /// <summary>
    /// Détruit les salles existantes avant d'en générer ou charger un nouvel étage
    /// </summary>
    void ClearOldData()
    {
        foreach (var kvp in roomPositions)
        {
            if (kvp.Value.roomObject != null)
                Destroy(kvp.Value.roomObject);
        }
        roomPositions.Clear();
        exportedRoomData.Clear();
        IsFloorReady = false;
    }
    #endregion
}

#region Data structures
[System.Serializable]
public class FloorSaveData
{
    public int floorIndex;
    public List<RoomExportData> rooms;
}

[System.Serializable]
public class RoomExportData
{
    public Vector2 position;
    public bool[]  walls;
    public int     partitionSeed;

    public RoomExportData(Vector2 pos, bool[] w, int seed)
    {
        position      = pos;
        walls         = w;
        partitionSeed = seed;
    }
}

public class RoomData
{
    public GameObject roomObject;
    public bool[] walls;

    public RoomData(GameObject roomObject, bool[] walls)
    {
        this.roomObject = roomObject;
        this.walls      = walls;
    }
}
#endregion
