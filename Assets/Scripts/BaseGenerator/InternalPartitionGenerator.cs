using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Génère et gère les cloisons internes d’une salle (sub‑rooms + portes).
/// </summary>
public class InternalPartitionGenerator : MonoBehaviour
{
    #region Inspector fields
    [Header("Prefabs des cloisons")]
    public GameObject[] wallPrefabsWithDoor; // Assigné dans l'inspecteur
    public GameObject[] wallPrefabsNoDoor;   // Assigné dans l'inspecteur

    [Header("Dimensions de la pièce (X,Z = taille au sol)")]
    public Vector3 roomDimensions = new Vector3(50f, 0.1f, 50f);

    [Header("Grille")]
    public int gridRows    = 5;
    public int gridColumns = 5;

    [Header("Partitions internes (comptes max)")]
    [Tooltip("Nombre de cloisons horizontales maximum (0 à gridRows‑1)")]
    public int horizontalPartitionsCount = 2;
    [Tooltip("Nombre de cloisons verticales maximum (0 à gridColumns‑1)")]
    public int verticalPartitionsCount   = 2;
    #endregion

    #region Runtime state
    private float cellWidth;
    private float cellHeight;

    private List<int> horizontalPartitions = new List<int>();
    private List<int> verticalPartitions   = new List<int>();

    private int[,] cellRoomIds;                    // [row,col] → roomId
    private readonly Dictionary<int, Color>           roomColors = new Dictionary<int, Color>();
    private readonly Dictionary<int, List<Vector2Int>> roomCells  = new Dictionary<int, List<Vector2Int>>();

    private readonly List<WallInfo> placedWalls = new List<WallInfo>();

    private int generationSeed = 0;
    public  void SetSeed(int seed) => generationSeed = seed;
    #endregion

    #region Nested types
    /// <summary>
    /// Encapsule un mur posé entre deux sous‑salles.
    /// </summary>
    private class WallInfo
    {
        public GameObject wallObject;
        public int roomA;
        public int roomB;
        public bool hasDoor;
        public bool isHorizontal; // true = horizontal, false = vertical
        public int  lineIndex;    // Ligne/colonne de la cloison
        public int  cellIndex;    // Cellule le long de la cloison
    }
    #endregion

    #region Unity lifecycle
    private void Start()
    {
        Random.InitState(generationSeed);

        cellWidth  = roomDimensions.x / gridColumns;
        cellHeight = roomDimensions.z / gridRows;

        // Choix aléatoire du nombre de cloisons à placer (au plus la valeur indiquée dans l'inspecteur)
        horizontalPartitionsCount = Random.Range(0, Mathf.Max(1, (gridRows    - 1) / 2 + 1));
        verticalPartitionsCount   = Random.Range(0, Mathf.Max(1, (gridColumns - 1) / 2 + 1));

        GenerateRandomPartitions();
        DetermineSubRooms();
        PlaceNoDoorWalls();
        ReplaceOneWallWithDoor();
        EnsureAllSubRoomsHaveDoor();
        EnsureFullConnectivity();
    }
    #endregion

    #region Generation steps
    private void GenerateRandomPartitions()
    {
        horizontalPartitions.Clear();
        verticalPartitions.Clear();

        // Lignes horizontales
        List<int> candidatesH = new List<int>();
        for (int i = 1; i < gridRows; i++) candidatesH.Add(i);
        Shuffle(candidatesH);
        for (int i = 0; i < Mathf.Min(horizontalPartitionsCount, candidatesH.Count); i++)
            horizontalPartitions.Add(candidatesH[i]);

        // Lignes verticales
        List<int> candidatesV = new List<int>();
        for (int j = 1; j < gridColumns; j++) candidatesV.Add(j);
        Shuffle(candidatesV);
        for (int j = 0; j < Mathf.Min(verticalPartitionsCount, candidatesV.Count); j++)
            verticalPartitions.Add(candidatesV[j]);
    }

    private void DetermineSubRooms()
    {
        cellRoomIds = new int[gridRows, gridColumns];
        horizontalPartitions.Sort();
        verticalPartitions.Sort();

        // Découpage en bandes horizontales et verticales
        var horizontalBands = new List<(int start, int end)>();
        {
            int start = 0;
            foreach (int h in horizontalPartitions)
            {
                horizontalBands.Add((start, h - 1));
                start = h;
            }
            horizontalBands.Add((start, gridRows - 1));
        }

        var verticalBands = new List<(int start, int end)>();
        {
            int start = 0;
            foreach (int v in verticalPartitions)
            {
                verticalBands.Add((start, v - 1));
                start = v;
            }
            verticalBands.Add((start, gridColumns - 1));
        }

        int roomId = 1;
        foreach (var hBand in horizontalBands)
        {
            foreach (var vBand in verticalBands)
            {
                for (int r = hBand.start; r <= hBand.end; r++)
                {
                    for (int c = vBand.start; c <= vBand.end; c++)
                    {
                        cellRoomIds[r, c] = roomId;
                    }
                }
                roomId++;
            }
        }

        // Stocke les cellules par sous‑salle
        roomCells.Clear();
        roomColors.Clear();
        for (int r = 0; r < gridRows; r++)
        {
            for (int c = 0; c < gridColumns; c++)
            {
                int rid = cellRoomIds[r, c];
                if (!roomCells.ContainsKey(rid)) roomCells[rid] = new List<Vector2Int>();
                roomCells[rid].Add(new Vector2Int(r, c));
            }
        }

        foreach (var kvp in roomCells)
            roomColors[kvp.Key] = Random.ColorHSV(0f, 1f, 0.5f, 1f, 0.5f, 1f);
    }

    private void PlaceNoDoorWalls()
    {
        placedWalls.Clear();

        // Cloisons horizontales
        foreach (int hLine in horizontalPartitions)
        {
            for (int c = 0; c < gridColumns; c++)
            {
                Vector3 pos = CellToWorldPosition(hLine, c, horizontal: true);
                Quaternion rot = Quaternion.identity;
                GameObject wallObj = InstantiateRandomNoDoorWall(pos, rot);
                if (wallObj == null) continue;

                int roomA = cellRoomIds[hLine - 1, c];
                int roomB = cellRoomIds[hLine,     c];

                placedWalls.Add(new WallInfo
                {
                    wallObject   = wallObj,
                    roomA        = roomA,
                    roomB        = roomB,
                    hasDoor      = false,
                    isHorizontal = true,
                    lineIndex    = hLine,
                    cellIndex    = c
                });
            }
        }

        // Cloisons verticales
        foreach (int vLine in verticalPartitions)
        {
            for (int r = 0; r < gridRows; r++)
            {
                Vector3 pos = CellToWorldPosition(r, vLine, horizontal: false);
                Quaternion rot = Quaternion.Euler(0f, 90f, 0f);
                GameObject wallObj = InstantiateRandomNoDoorWall(pos, rot);
                if (wallObj == null) continue;

                int roomA = cellRoomIds[r, vLine - 1];
                int roomB = cellRoomIds[r, vLine];

                placedWalls.Add(new WallInfo
                {
                    wallObject   = wallObj,
                    roomA        = roomA,
                    roomB        = roomB,
                    hasDoor      = false,
                    isHorizontal = false,
                    lineIndex    = vLine,
                    cellIndex    = r
                });
            }
        }
    }

    private void ReplaceOneWallWithDoor()
    {
        if (placedWalls.Count == 0 || wallPrefabsWithDoor == null || wallPrefabsWithDoor.Length == 0) return;

        int idx = Random.Range(0, placedWalls.Count);
        WallInfo w = placedWalls[idx];

        Vector3 pos = w.wallObject.transform.position;
        Quaternion rot = w.wallObject.transform.rotation;
        Destroy(w.wallObject);
        placedWalls.RemoveAt(idx);

        GameObject prefab = wallPrefabsWithDoor[Random.Range(0, wallPrefabsWithDoor.Length)];
        GameObject newWall = Instantiate(prefab, pos, rot, transform);

        w.wallObject = newWall;
        w.hasDoor    = true;
        placedWalls.Add(w);
    }

    private void EnsureAllSubRoomsHaveDoor()
    {
        var roomToWalls = BuildRoomToWallsDict();
        foreach (int roomId in roomCells.Keys)
        {
            if (roomId == 0 || !roomToWalls.ContainsKey(roomId)) continue;
            bool hasDoor = roomToWalls[roomId].Exists(w => w.hasDoor);
            if (hasDoor) continue;

            // Choisir la première cloison sans porte et la transformer
            WallInfo candidate = roomToWalls[roomId].Find(w => !w.hasDoor);
            if (candidate != null) AddDoorToWall(candidate);
        }
    }

    private void EnsureFullConnectivity()
    {
        var rooms = new List<int>(roomCells.Keys);
        if (rooms.Count <= 1) return;

        var roomToWalls = BuildRoomToWallsDict();

        // Union‑Find
        var parent = new Dictionary<int, int>();
        foreach (int r in rooms) parent[r] = r;

        System.Func<int, int> find = null;
        find = (x) => parent[x] == x ? x : (parent[x] = find(parent[x]));

        System.Action<int, int> unionSet = (a, b) =>
        {
            a = find(a);
            b = find(b);
            if (a != b) parent[b] = a;
        };

        // Unir les salles déjà connectées par des portes
        foreach (WallInfo w in placedWalls) if (w.hasDoor) unionSet(w.roomA, w.roomB);

        // Tant qu’il existe plusieurs composantes, on ajoute des portes
        while (!AllInOneSet(rooms, find))
        {
            WallInfo candidate = placedWalls.Find(w => !w.hasDoor && find(w.roomA) != find(w.roomB));
            if (candidate == null)
            {
                Debug.LogWarning("Impossible de connecter toutes les sous‑salles");
                break;
            }
            AddDoorToWall(candidate);
            unionSet(candidate.roomA, candidate.roomB);
        }
    }
    #endregion

    #region Export / Import
    /// <summary>
    /// Export minimal pour la sauvegarde JSON.
    /// </summary>
    public PartitionSaveData ExportPartition()
    {
        return new PartitionSaveData
        {
            horizontalPartitions = new List<int>(horizontalPartitions),
            verticalPartitions   = new List<int>(verticalPartitions),
            doors = placedWalls
                .FindAll(w => w.hasDoor)
                .ConvertAll<WallDoorInfo>(w => new WallDoorInfo
                {
                    isHorizontal = w.isHorizontal,
                    lineIndex    = w.lineIndex,
                    cellIndex    = w.cellIndex
                })
        };
    }

    /// <summary>
    /// Reconstruit la configuration à partir de données sauvegardées.
    /// </summary>
    public void ImportPartition(PartitionSaveData data)
    {
        horizontalPartitions = new List<int>(data.horizontalPartitions);
        verticalPartitions   = new List<int>(data.verticalPartitions);

        DetermineSubRooms();
        PlaceNoDoorWalls();

        // Re‑place exactement les mêmes portes
        foreach (WallDoorInfo d in data.doors)
        {
            WallInfo w = placedWalls.Find(p =>
                p.isHorizontal == d.isHorizontal &&
                p.lineIndex    == d.lineIndex    &&
                p.cellIndex    == d.cellIndex);
            if (w != null && !w.hasDoor) AddDoorToWall(w);
        }
    }
    #endregion

    #region Helpers
    private bool AllInOneSet(List<int> rooms, System.Func<int, int> find)
    {
        int root = find(rooms[0]);
        for (int i = 1; i < rooms.Count; i++) if (find(rooms[i]) != root) return false;
        return true;
    }

    private void AddDoorToWall(WallInfo w)
    {
        if (wallPrefabsWithDoor == null || wallPrefabsWithDoor.Length == 0) return;
        Vector3 pos = w.wallObject.transform.position;
        Quaternion rot = w.wallObject.transform.rotation;
        Destroy(w.wallObject);
        GameObject prefab = wallPrefabsWithDoor[Random.Range(0, wallPrefabsWithDoor.Length)];
        w.wallObject = Instantiate(prefab, pos, rot, transform);
        w.hasDoor    = true;
    }

    private Dictionary<int, List<WallInfo>> BuildRoomToWallsDict()
    {
        var dict = new Dictionary<int, List<WallInfo>>();
        foreach (WallInfo w in placedWalls)
        {
            if (!dict.ContainsKey(w.roomA)) dict[w.roomA] = new List<WallInfo>();
            if (!dict.ContainsKey(w.roomB)) dict[w.roomB] = new List<WallInfo>();
            dict[w.roomA].Add(w);
            dict[w.roomB].Add(w);
        }
        return dict;
    }

    private GameObject InstantiateRandomNoDoorWall(Vector3 position, Quaternion rotation)
    {
        if (wallPrefabsNoDoor == null || wallPrefabsNoDoor.Length == 0)
        {
            Debug.LogWarning("Aucun prefab de mur sans porte disponible.");
            return null;
        }
        GameObject prefab = wallPrefabsNoDoor[Random.Range(0, wallPrefabsNoDoor.Length)];
        return Instantiate(prefab, position, rotation, transform);
    }

    private Vector3 CellToWorldPosition(int row, int col, bool horizontal)
    {
        float yPos = 3f;
        if (horizontal)
        {
            float lineZ = -roomDimensions.z * 0.5f + row * cellHeight;
            float colX  = -roomDimensions.x * 0.5f + col * cellWidth + cellWidth * 0.5f;
            return transform.TransformPoint(new Vector3(colX, yPos, lineZ));
        }
        else
        {
            float lineX = -roomDimensions.x * 0.5f + col * cellWidth;
            float rowZ  = -roomDimensions.z * 0.5f + row * cellHeight + cellHeight * 0.5f;
            return transform.TransformPoint(new Vector3(lineX, yPos, rowZ));
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }
    #endregion

    #region Debug / Gizmos
    private void OnDrawGizmos()
    {
        // Limite générale
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position + new Vector3(0, roomDimensions.y * 0.5f, 0), roomDimensions);

        if (Application.isPlaying && cellRoomIds != null)
        {
            for (int r = 0; r < gridRows; r++)
            {
                for (int c = 0; c < gridColumns; c++)
                {
                    int rid = cellRoomIds[r, c];
                    Gizmos.color = roomColors.ContainsKey(rid) ? roomColors[rid] : Color.gray;
                    Vector3 center = transform.position + new Vector3(
                        (-roomDimensions.x * 0.5f) + c * cellWidth + cellWidth * 0.5f,
                        0f,
                        (-roomDimensions.z * 0.5f) + r * cellHeight + cellHeight * 0.5f);
                    Gizmos.DrawCube(center, new Vector3(cellWidth, 0.01f, cellHeight));
                }
            }
        }

        // Grille
        if (gridRows > 0 && gridColumns > 0)
        {
            Gizmos.color = Color.yellow;
            float halfX = roomDimensions.x * 0.5f;
            float halfZ = roomDimensions.z * 0.5f;
            float rowStep = roomDimensions.z / gridRows;
            float colStep = roomDimensions.x / gridColumns;

            for (int i = 0; i <= gridRows; i++)
            {
                Vector3 a = transform.position + new Vector3(-halfX, 0, -halfZ + i * rowStep);
                Vector3 b = transform.position + new Vector3( halfX, 0, -halfZ + i * rowStep);
                Gizmos.DrawLine(a, b);
            }
            for (int j = 0; j <= gridColumns; j++)
            {
                Vector3 a = transform.position + new Vector3(-halfX + j * colStep, 0, -halfZ);
                Vector3 b = transform.position + new Vector3(-halfX + j * colStep, 0,  halfZ);
                Gizmos.DrawLine(a, b);
            }
        }
    }
    #endregion
}

// ============================================================================
// Structures sérialisables pour la sauvegarde JSON
// ----------------------------------------------------------------------------

[System.Serializable]
public class WallDoorInfo
{
    public bool isHorizontal;
    public int  lineIndex;
    public int  cellIndex;
}

[System.Serializable]
public class PartitionSaveData
{
    public List<int> horizontalPartitions;
    public List<int> verticalPartitions;
    public List<WallDoorInfo> doors;
}
