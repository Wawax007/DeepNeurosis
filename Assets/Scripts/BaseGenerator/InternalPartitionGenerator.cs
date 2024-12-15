using UnityEngine;
using System.Collections.Generic;

public class InternalPartitionGenerator : MonoBehaviour
{
    [Header("Prefabs des cloisons")]
    public GameObject[] wallPrefabsWithDoor; // Assigné dans l'inspecteur
    public GameObject[] wallPrefabsNoDoor;   // Assigné dans l'inspecteur

    [Header("Paramètres de la pièce")]
    public Vector3 roomDimensions = new Vector3(50f, 0.1f, 50f);

    [Header("Grille")]
    public int gridRows = 5;
    public int gridColumns = 5;

    [Header("Génération de partitions")]
    [Tooltip("Nombre de partitions internes horizontales à créer (0 à gridRows-1)")]
    public int horizontalPartitionsCount = 2;
    [Tooltip("Nombre de partitions internes verticales à créer (0 à gridColumns-1)")]
    public int verticalPartitionsCount = 2;

    private float cellWidth;
    private float cellHeight;

    private List<int> horizontalPartitions = new List<int>();
    private List<int> verticalPartitions = new List<int>();

    private int[,] cellRoomIds;
    private Dictionary<int, Color> roomColors = new Dictionary<int, Color>();
    private Dictionary<int, List<Vector2Int>> roomCells = new Dictionary<int, List<Vector2Int>>();

    // Pour représenter un mur placé
    private class WallInfo
    {
        public GameObject wallObject;
        public int roomA;
        public int roomB;
        public bool hasDoor;
        public bool isHorizontal; // true si mur horizontal, false si vertical
        public int lineIndex;    // index de la ligne de partition
        public int cellIndex;    // index de la colonne ou ligne selon le sens
    }

    // Murs placés
    private List<WallInfo> placedWalls = new List<WallInfo>();

    private void Start()
    {
        cellWidth = roomDimensions.x / gridColumns;   
        cellHeight = roomDimensions.z / gridRows;     

        // On randomise le nombre de partitions internes
        horizontalPartitionsCount = Random.Range(0, Mathf.Max(1, (gridRows - 1)/2 + 1)); 
        verticalPartitionsCount   = Random.Range(0, Mathf.Max(1, (gridColumns - 1)/2 + 1));

        // Générer des partitions internes
        GenerateRandomPartitions();

        // Déterminer les sous-salles
        DetermineSubRooms();

        // Placer les cloisons sans porte
        PlaceNoDoorWalls();

        // Remplacer un mur par une cloison avec porte (au hasard)
        ReplaceOneWallWithDoor();

        // Vérification finale : s'assurer que toutes les sous-salles ont au moins une cloison avec porte
        EnsureAllSubRoomsHaveDoor();

        // Nouvelle étape : s'assurer que toutes les sous-salles sont accessibles entre elles (connectivité globale)
        EnsureFullConnectivity();
    }

    private void GenerateRandomPartitions()
    {
        horizontalPartitions.Clear();
        verticalPartitions.Clear();

        // Partitions horizontales
        List<int> possibleH = new List<int>();
        for (int i = 1; i < gridRows; i++)
            possibleH.Add(i);
        Shuffle(possibleH);
        for (int i = 0; i < Mathf.Min(horizontalPartitionsCount, possibleH.Count); i++)
            horizontalPartitions.Add(possibleH[i]);

        // Partitions verticales
        List<int> possibleV = new List<int>();
        for (int j = 1; j < gridColumns; j++)
            possibleV.Add(j);
        Shuffle(possibleV);
        for (int j = 0; j < Mathf.Min(verticalPartitionsCount, possibleV.Count); j++)
            verticalPartitions.Add(possibleV[j]);
    }

    private void DetermineSubRooms()
    {
        cellRoomIds = new int[gridRows, gridColumns];
        horizontalPartitions.Sort();
        verticalPartitions.Sort();

        List<(int start, int end)> horizontalBands = new List<(int, int)>();
        {
            int start = 0;
            foreach (int h in horizontalPartitions)
            {
                horizontalBands.Add((start, h-1));
                start = h;
            }
            horizontalBands.Add((start, gridRows-1));
        }

        List<(int start, int end)> verticalBands = new List<(int, int)>();
        {
            int start = 0;
            foreach (int v in verticalPartitions)
            {
                verticalBands.Add((start, v-1));
                start = v;
            }
            verticalBands.Add((start, gridColumns-1));
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
                        cellRoomIds[r,c] = roomId;
                    }
                }

                roomId++;
            }
        }

        roomCells.Clear();
        roomColors.Clear();

        for (int r = 0; r < gridRows; r++)
        {
            for (int c = 0; c < gridColumns; c++)
            {
                int rid = cellRoomIds[r,c];
                if (!roomCells.ContainsKey(rid))
                    roomCells[rid] = new List<Vector2Int>();
                roomCells[rid].Add(new Vector2Int(r,c));
            }
        }

        foreach (var kvp in roomCells)
        {
            roomColors[kvp.Key] = Random.ColorHSV(0f,1f,0.5f,1f,0.5f,1f);
        }
    }

    private void PlaceNoDoorWalls()
    {
        placedWalls.Clear();

        // Murs horizontaux
        foreach (int hLine in horizontalPartitions)
        {
            for (int c = 0; c < gridColumns; c++)
            {
                Vector3 position = CellToWorldPosition(hLine, c, horizontal: true);
                Quaternion rotation = Quaternion.identity;
                var wallObj = InstantiateRandomNoDoorWall(position, rotation);
                if (wallObj != null)
                {
                    int roomA = cellRoomIds[hLine-1, c];
                    int roomB = cellRoomIds[hLine, c];

                    placedWalls.Add(new WallInfo {
                        wallObject = wallObj,
                        roomA = roomA,
                        roomB = roomB,
                        hasDoor = false,
                        isHorizontal = true,
                        lineIndex = hLine,
                        cellIndex = c
                    });
                }
            }
        }

        // Murs verticaux
        foreach (int vLine in verticalPartitions)
        {
            for (int r = 0; r < gridRows; r++)
            {
                Vector3 position = CellToWorldPosition(r, vLine, horizontal: false);
                Quaternion rotation = Quaternion.Euler(0f,90f,0f);
                var wallObj = InstantiateRandomNoDoorWall(position, rotation);
                if (wallObj != null)
                {
                    int roomA = cellRoomIds[r, vLine-1];
                    int roomB = cellRoomIds[r, vLine];

                    placedWalls.Add(new WallInfo {
                        wallObject = wallObj,
                        roomA = roomA,
                        roomB = roomB,
                        hasDoor = false,
                        isHorizontal = false,
                        lineIndex = vLine,
                        cellIndex = r
                    });
                }
            }
        }
    }

    private void ReplaceOneWallWithDoor()
    {
        if (placedWalls.Count == 0 || wallPrefabsWithDoor == null || wallPrefabsWithDoor.Length == 0)
            return;

        int index = Random.Range(0, placedWalls.Count);
        WallInfo chosenWall = placedWalls[index];

        Vector3 pos = chosenWall.wallObject.transform.position;
        Quaternion rot = chosenWall.wallObject.transform.rotation;

        Destroy(chosenWall.wallObject);
        placedWalls.RemoveAt(index);

        GameObject doorWallPrefab = wallPrefabsWithDoor[Random.Range(0, wallPrefabsWithDoor.Length)];
        GameObject newWall = Instantiate(doorWallPrefab, pos, rot, transform);

        chosenWall.wallObject = newWall;
        chosenWall.hasDoor = true;
        placedWalls.Add(chosenWall);
    }

    private void EnsureAllSubRoomsHaveDoor()
    {
        Dictionary<int, List<WallInfo>> roomToWalls = BuildRoomToWallsDict();

        foreach (var kvp in roomCells)
        {
            int roomId = kvp.Key;
            if (roomId == 0) continue;

            if (!roomToWalls.ContainsKey(roomId)) continue;

            bool hasDoor = false;
            foreach (var w in roomToWalls[roomId])
            {
                if (w.hasDoor)
                {
                    hasDoor = true;
                    break;
                }
            }

            if (!hasDoor)
            {
                // Pas de porte, on en ajoute une
                WallInfo candidate = null;
                foreach (var w in roomToWalls[roomId])
                {
                    if (!w.hasDoor)
                    {
                        candidate = w;
                        break;
                    }
                }

                if (candidate != null && wallPrefabsWithDoor.Length > 0)
                {
                    Vector3 pos = candidate.wallObject.transform.position;
                    Quaternion rot = candidate.wallObject.transform.rotation;

                    Destroy(candidate.wallObject);

                    GameObject doorWallPrefab = wallPrefabsWithDoor[Random.Range(0, wallPrefabsWithDoor.Length)];
                    GameObject newWall = Instantiate(doorWallPrefab, pos, rot, transform);

                    candidate.wallObject = newWall;
                    candidate.hasDoor = true;
                }
            }
        }
    }

    private void EnsureFullConnectivity()
    {
        // On utilise un Union-Find (ou Disjoint Set) pour vérifier la connectivité.
        var rooms = new List<int>(roomCells.Keys);
        if (rooms.Count <= 1) return; // Une seule salle ou aucune, déjà connecté

        Dictionary<int, List<WallInfo>> roomToWalls = BuildRoomToWallsDict();

        // Initialiser le Union-Find
        var parent = new Dictionary<int, int>();
        foreach (var r in rooms)
            parent[r] = r;

        System.Func<int,int> find = null;
        find = (x) => (parent[x] == x) ? x : (parent[x] = find(parent[x]));

        System.Action<int,int> unionSet = (a,b) => {
            a = find(a);
            b = find(b);
            if (a != b) parent[b] = a;
        };

        // Unir toutes les salles déjà connectées par des portes
        foreach (var w in placedWalls)
        {
            if (w.hasDoor)
                unionSet(w.roomA, w.roomB);
        }

        // Vérifier si tout est dans la même composante
        // Si non, on tente d'ajouter des portes jusqu'à ce que ce soit le cas ou qu'on manque de possibilités.
        while (!AllInOneSet(rooms, find))
        {
            // On a plusieurs composantes, on va essayer de connecter deux composantes en ajoutant une porte
            // Sélectionner un mur sans porte qui sépare deux salles de composantes différentes
            WallInfo candidate = null;
            foreach (var w in placedWalls)
            {
                if (!w.hasDoor)
                {
                    int ra = find(w.roomA);
                    int rb = find(w.roomB);
                    if (ra != rb)
                    {
                        candidate = w;
                        break;
                    }
                }
            }

            if (candidate == null)
            {
                // Plus de mur possible pour connecter les composantes, on abandonne
                // Cela signifie qu'il existe des parties inatteignables.
                Debug.LogWarning("Impossible de connecter toutes les salles, certaines zones resteront inexplorables.");
                break;
            }
            else
            {
                // On ajoute une porte dans ce mur pour connecter ces deux composantes
                AddDoorToWall(candidate);
                // Unir les composantes
                unionSet(candidate.roomA, candidate.roomB);
            }
        }
    }

    private bool AllInOneSet(List<int> rooms, System.Func<int,int> find)
    {
        int root = find(rooms[0]);
        for (int i = 1; i < rooms.Count; i++)
        {
            if (find(rooms[i]) != root)
                return false;
        }
        return true;
    }

    private void AddDoorToWall(WallInfo w)
    {
        if (wallPrefabsWithDoor == null || wallPrefabsWithDoor.Length == 0) return;
        Vector3 pos = w.wallObject.transform.position;
        Quaternion rot = w.wallObject.transform.rotation;

        Destroy(w.wallObject);

        GameObject doorWallPrefab = wallPrefabsWithDoor[Random.Range(0, wallPrefabsWithDoor.Length)];
        GameObject newWall = Instantiate(doorWallPrefab, pos, rot, transform);

        w.wallObject = newWall;
        w.hasDoor = true;
    }

    private Dictionary<int, List<WallInfo>> BuildRoomToWallsDict()
    {
        Dictionary<int, List<WallInfo>> roomToWalls = new Dictionary<int, List<WallInfo>>();
        foreach (var w in placedWalls)
        {
            if (!roomToWalls.ContainsKey(w.roomA))
                roomToWalls[w.roomA] = new List<WallInfo>();
            if (!roomToWalls.ContainsKey(w.roomB))
                roomToWalls[w.roomB] = new List<WallInfo>();

            roomToWalls[w.roomA].Add(w);
            roomToWalls[w.roomB].Add(w);
        }
        return roomToWalls;
    }

    private GameObject InstantiateRandomNoDoorWall(Vector3 position, Quaternion rotation)
    {
        if (wallPrefabsNoDoor == null || wallPrefabsNoDoor.Length == 0)
        {
            Debug.LogWarning("Aucun prefab de cloison sans porte disponible.");
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
            float colX = -roomDimensions.x * 0.5f + col * cellWidth + cellWidth * 0.5f;
            return transform.TransformPoint(new Vector3(colX, yPos, lineZ));
        }
        else
        {
            float lineX = -roomDimensions.x * 0.5f + col * cellWidth;
            float rowZ = -roomDimensions.z * 0.5f + row * cellHeight + cellHeight * 0.5f;
            return transform.TransformPoint(new Vector3(lineX, yPos, rowZ));
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            T tmp = list[i];
            list[i] = list[r];
            list[r] = tmp;
        }
    }

    private void OnDrawGizmos()
    {
        // Dessine la limite de la pièce
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position + new Vector3(0, roomDimensions.y * 0.5f, 0), roomDimensions);

        if (Application.isPlaying && cellRoomIds != null)
        {
            // Afficher les sous-salles en couleur
            for (int r = 0; r < gridRows; r++)
            {
                for (int c = 0; c < gridColumns; c++)
                {
                    int rid = cellRoomIds[r,c];
                    if (roomColors.ContainsKey(rid))
                    {
                        Gizmos.color = roomColors[rid];
                    }
                    else
                    {
                        Gizmos.color = Color.gray;
                    }

                    Vector3 center = transform.position + new Vector3(
                        (-roomDimensions.x * 0.5f) + c * cellWidth + cellWidth * 0.5f,
                        0f,
                        (-roomDimensions.z * 0.5f) + r * cellHeight + cellHeight * 0.5f
                    );

                    Gizmos.DrawCube(center, new Vector3(cellWidth, 0.01f, cellHeight));
                }
            }
        }

        // Dessine la grille
        if (gridRows > 0 && gridColumns > 0)
        {
            Gizmos.color = Color.yellow;
            float halfX = roomDimensions.x * 0.5f;
            float halfZ = roomDimensions.z * 0.5f;
            float rowStep = roomDimensions.z / gridRows;
            float colStep = roomDimensions.x / gridColumns;

            // Lignes horizontales
            for (int i = 0; i <= gridRows; i++)
            {
                Vector3 start = transform.position + new Vector3(-halfX, 0f, -halfZ + i * rowStep);
                Vector3 end = transform.position + new Vector3(halfX, 0f, -halfZ + i * rowStep);
                Gizmos.DrawLine(start, end);
            }

            // Lignes verticales
            for (int j = 0; j <= gridColumns; j++)
            {
                Vector3 start = transform.position + new Vector3(-halfX + j * colStep, 0f, -halfZ);
                Vector3 end = transform.position + new Vector3(-halfX + j * colStep, 0f, halfZ);
                Gizmos.DrawLine(start, end);
            }
        }
    }
}
