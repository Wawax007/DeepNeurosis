using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;

/// <summary>
/// Gère la génération procédurale d'un "étage" (un ensemble de salles).
/// </summary>
public class BaseGenerator : MonoBehaviour
{
    [Header("Prefabs & Settings")]
    public GameObject roomPrefab;
    public int numberOfRooms = 5;
    public float roomSize = 50f;

    // Le point d’ancrage où est placé l’ascenseur
    // => on le place exactement à la position de l’ascenseur, de sorte
    //    que le bord Sud de la salle (room) coïncide avec l’ascenseur.
    public Vector3 floorAnchor = new Vector3(183.021194f, 0f, -4.9f);

    // Données internes de l’étage
    private Dictionary<Vector2, RoomData> roomPositions = new Dictionary<Vector2, RoomData>();
    private List<RoomExportData> exportedRoomData = new List<RoomExportData>();

    public bool IsFloorReady { get; private set; }

    /// <summary>
    /// Génère un nouvel étage "propre" de façon procédurale.
    /// </summary>
    public void GenerateFloor(int floorIndex)
    {
        ClearOldData();
        Debug.Log($"[BaseGenerator] Generating floor {floorIndex} ...");
        StartCoroutine(GenerateMapCoroutine());
    }

    /// <summary>
    /// Construit un étage depuis des données chargées (FloorSaveData).
    /// </summary>
    public void LoadFloorFromData(FloorSaveData floorData)
    {
        ClearOldData();
        Debug.Log($"[BaseGenerator] Loading floor from JSON data (floorIndex: {floorData.floorIndex})");

        // Reconstruit toutes les salles depuis le JSON
        foreach (var roomInfo in floorData.rooms)
        {
            Vector2 pos = roomInfo.position;
            GameObject roomObj = InstantiateRoom(pos);
            RoomData rData = new RoomData(roomObj, roomInfo.walls);
            roomPositions.Add(pos, rData);
            exportedRoomData.Add(new RoomExportData(pos, roomInfo.walls));
        }

        // Ajuste murs/portes
        AdjustRoomWalls();
        CheckDoorErrors();

        IsFloorReady = true;
    }

    IEnumerator GenerateMapCoroutine()
    {
        // La salle initiale est à la coordonnée (0,0) dans notre grille
        Vector2 startGridPos = Vector2.zero;
        Debug.Log($"Generating first room at grid {startGridPos}");

        // Instancie la 1ère salle
        GameObject startRoom = InstantiateRoom(startGridPos);
        RoomData startRoomData = new RoomData(startRoom, new bool[4]);
        roomPositions.Add(startGridPos, startRoomData);

        // On stocke l’info des murs
        exportedRoomData.Add(new RoomExportData(startGridPos, startRoomData.walls));

        // Génère d’autres salles (aléatoires) reliées par adjacence
        for (int i = 1; i < numberOfRooms; i++)
        {
            Vector2 roomPos;
            bool valid;

            do
            {
                roomPos = GetRandomAdjacentPosition();
                valid = !roomPositions.ContainsKey(roomPos);
            }
            while (!valid);

            GameObject newRoom = InstantiateRoom(roomPos);
            roomPositions.Add(roomPos, new RoomData(newRoom, new bool[4]));
            exportedRoomData.Add(new RoomExportData(roomPos, new bool[4]));
        }

        // Ajuste murs/portes
        AdjustRoomWalls();
        CheckDoorErrors();

        IsFloorReady = true;
        yield break;
    }

    /// <summary>
    /// Calcule les murs/portes internes/externes
    /// </summary>
    void AdjustRoomWalls()
    {
        foreach (var entry in roomPositions)
        {
            Vector2 pos = entry.Key;
            RoomData roomData = entry.Value;

            // Vérifie la présence de voisins
            bool northExt = !roomPositions.ContainsKey(pos + Vector2.up);
            bool southExt = !roomPositions.ContainsKey(pos + Vector2.down);
            bool eastExt  = !roomPositions.ContainsKey(pos + Vector2.right);
            bool westExt  = !roomPositions.ContainsKey(pos + Vector2.left);

            roomData.walls[0] = northExt; // 0 => N
            roomData.walls[1] = southExt; // 1 => S
            roomData.walls[2] = eastExt;  // 2 => E
            roomData.walls[3] = westExt;  // 3 => W

            bool northDoor = false, southDoor = false, eastDoor = false, westDoor = false;
            bool placeN = true, placeS = true, placeE = true, placeW = true;

            // Nord
            if (roomPositions.ContainsKey(pos + Vector2.up))
            {
                if (pos.y < (pos + Vector2.up).y)
                    northDoor = true;   // c'est nous qui plaçons la porte
                else
                    placeN = false;    // c'est l'autre salle qui le fait
            }
            // Sud
            if (roomPositions.ContainsKey(pos + Vector2.down))
            {
                if (pos.y < (pos + Vector2.down).y)
                    southDoor = true;
                else
                    placeS = false;
            }
            // Est
            if (roomPositions.ContainsKey(pos + Vector2.right))
            {
                if (pos.x < (pos + Vector2.right).x)
                    eastDoor = true;
                else
                    placeE = false;
            }
            // Ouest
            if (roomPositions.ContainsKey(pos + Vector2.left))
            {
                if (pos.x < (pos + Vector2.left).x)
                    westDoor = true;
                else
                    placeW = false;
            }

            // Pour la première salle (0,0), on veut supprimer le mur Sud (ou placer une "ouverture") pour
            // qu’elle soit connectée à l’ascenseur. 
            if (pos == Vector2.zero)
            {
                placeS = false;
                southDoor = false; // pas de porte décorative
                roomData.walls[1] = false; // Indique "pas de mur" au Sud
            }

            // Lance la config
            RoomGenerator roomGen = roomData.roomObject.GetComponent<RoomGenerator>();
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
    /// Instancie une salle selon sa position "grille" relative
    /// en la plaçant devant l’ascenseur.
    /// </summary>
    private GameObject InstantiateRoom(Vector2 gridPos)
    {
        float half = roomSize * 0.5f;
        // On ajoute half sur Z pour que le bord Sud colle à ascenseur.z
        Vector3 offset = new Vector3(
            gridPos.x * roomSize,
            0f,
            (gridPos.y * roomSize) + half
        );

        // On place la salle
        Vector3 worldPos = floorAnchor + offset;

        // Instantie
        return Instantiate(roomPrefab, worldPos, Quaternion.identity, this.transform);
    }

    /// <summary>
    /// Trouve au hasard une position voisine d'une salle existante
    /// (on retire Vector2.down pour éviter de construire derrière l'ascenseur).
    /// </summary>
    Vector2 GetRandomAdjacentPosition()
    {
        // On ne propose plus 'down', donc impossible d'aller au sud
        List<Vector2> directions = new List<Vector2> 
        { 
            Vector2.up, 
            Vector2.left, 
            Vector2.right
        };

        Vector2 randomDir = directions[Random.Range(0, directions.Count)];
        Vector2 existingPos = new List<Vector2>(roomPositions.Keys)[Random.Range(0, roomPositions.Count)];
        return existingPos + randomDir;
    }

    /// <summary>
    /// Vérification d’éventuels conflits de murs
    /// </summary>
    void CheckDoorErrors()
    {
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

    /// <summary>
    /// Prépare les infos pour la sauvegarde JSON
    /// </summary>
    public FloorSaveData GetFloorData(int floorIndex)
    {
        FloorSaveData data = new FloorSaveData();
        data.floorIndex = floorIndex;
        data.rooms = new List<RoomExportData>();

        foreach (var kvp in roomPositions)
        {
            data.rooms.Add(new RoomExportData(kvp.Key, kvp.Value.walls));
        }
        return data;
    }
}

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
