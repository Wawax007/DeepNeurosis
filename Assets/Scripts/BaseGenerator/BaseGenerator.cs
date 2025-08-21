using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExtractionConsole.Module;
using Unity.AI.Navigation;
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

    [SerializeField]
    private int floorIndex;
    public int GetFloorIndex() => floorIndex;
    public void SetFloorIndex(int value) => floorIndex = value;

    [Header("Mob Settings")]
    public GameObject mobPrefab;
    public Vector3 mobOffset = new Vector3(0f, 0f, 0f);
    private GameObject spawnedMob;
    public NavMeshSurface navMeshSurface;
    
    public bool IsFloorReady { get; set; }

    #region Public API
    /// <summary>
    /// Génère un nouvel étage "propre" de façon procédurale.
    /// </summary>
    public void GenerateFloor(int floorIndex)
    {
        this.floorIndex = floorIndex;

        if (floorIndex == -2 || floorIndex == 2)
        {
            Debug.LogWarning("[BaseGenerator] StartRoom should not be procedurally generated.");
            IsFloorReady = true; // Évite un blocage d'attente
            return;
        }
        if (PropTransferManager.Instance != null)
        {
            PropTransferManager.Instance.MarkPropsInElevatorForPreservation();
        }      
        // Assure que la génération est active pour une génération procédurale
        PropPlacer.SkipGeneration = false;
        ClearOldData();
        Debug.Log($"[BaseGenerator] Generating floor {floorIndex} …");
        StartCoroutine(GenerateMapCoroutine(floorIndex));
    }


    /// <summary>
    /// Construit un étage depuis des données sauvegardées.
    /// </summary>
    public void LoadFloorFromData(FloorSaveData floorData)
    {
        this.floorIndex = floorData.floorIndex;
        if (PropTransferManager.Instance != null)
        {
            PropTransferManager.Instance.MarkPropsInElevatorForPreservation();
        }
        // Empêche la génération automatique de props par les RoomGenerator/PropPlacer
        PropPlacer.SkipGeneration = true;
        ClearOldData();
        PropPlacer.tempPlacedClues.RemoveAll(c => c.currentFloorIndex == floorIndex);

        Debug.Log($"[BaseGenerator] Loading floor from JSON data (floorIndex: {floorData.floorIndex})");

        foreach (var roomInfo in floorData.rooms)
        {
            Vector2 pos = roomInfo.position;
            GameObject room = InstantiateRoom(pos, floorData.floorIndex);

            var partGen = room.GetComponent<InternalPartitionGenerator>();
            if (partGen != null) partGen.SetSeed(roomInfo.partitionSeed);

            bool[] walls = (bool[])roomInfo.walls.Clone();
            roomPositions.Add(pos, new RoomData(room, walls));
            exportedRoomData.Add(new RoomExportData(pos, walls, roomInfo.partitionSeed));
        }

        AdjustRoomWalls();
        CheckDoorErrors();
        
        // Construit une table nom→prefab depuis tous les PropPlacer présents dans les rooms chargées
        var prefabByName = new Dictionary<string, GameObject>();
        foreach (var kv in roomPositions)
        {
            var roomObj = kv.Value.roomObject;
            foreach (var placer in roomObj.GetComponentsInChildren<PropPlacer>(true))
            {
                if (placer.microProps != null)
                {
                    foreach (var p in placer.microProps)
                    {
                        if (p == null) continue;
                        var key = p.name;
                        if (!prefabByName.ContainsKey(key)) prefabByName[key] = p;
                    }
                }
                if (placer.accentProps != null)
                {
                    foreach (var p in placer.accentProps)
                    {
                        if (p == null) continue;
                        var key = p.name;
                        if (!prefabByName.ContainsKey(key)) prefabByName[key] = p;
                    }
                }
                if (placer.clusterLibrary != null)
                {
                    foreach (var cluster in placer.clusterLibrary)
                    {
                        if (cluster == null || cluster.variants == null) continue;
                        foreach (var v in cluster.variants)
                        {
                            if (v == null) continue;
                            var key = v.name;
                            if (!prefabByName.ContainsKey(key)) prefabByName[key] = v;
                        }
                    }
                }
            }
        }
        

        foreach (var clue in floorData.placedClueProps)
        {
            var prefab = Resources.Load<GameObject>("CluePrefabs/" + clue.prefabName);
            if (prefab == null)
            {
                Debug.LogWarning($"[BaseGenerator] Prefab '{clue.prefabName}' introuvable dans Resources/CluePrefabs/");
                continue;
            }

            GameObject instance = Instantiate(prefab, clue.position, clue.rotation, transform);

            var moduleItem = instance.GetComponent<ModuleItem>();
            bool mustBeInserted = false;

            if (!string.IsNullOrEmpty(clue.moduleType) && floorData.consoleState != null)
            {
                if (clue.moduleType == "Security" && floorData.consoleState.securityInserted)
                    mustBeInserted = true;
                else if (clue.moduleType == "Navigation" && floorData.consoleState.navigationInserted)
                    mustBeInserted = true;
            }

            if (mustBeInserted)
            {
                var allSockets = GameObject.FindObjectsOfType<ModuleSocket>();
                foreach (var socket in allSockets)
                {
                    if (socket.requiredType.ToString() == clue.moduleType)
                    {
                        if (moduleItem != null)
                        {
                            socket.ForceFill(instance.GetComponent<Collider>());
                            socket.InitDiode();
                            CluePropTracker.MarkUsed(clue.enigmaId);
                        }
                        else
                        {
                            Debug.LogWarning($"[LoadFloorFromData] Module {clue.moduleType} marqué comme inséré mais prefab sans ModuleItem.");
                        }

                        break;
                    }
                }
            }
            else
            {
                var rb = instance.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = false;
                var col = instance.GetComponent<Collider>();
                if (col != null) col.enabled = true;
            }


            var saved = new SavedClueProp {
                enigmaId = clue.enigmaId,
                prefabName = clue.prefabName,
                position = clue.position,
                rotation = clue.rotation,
                used = clue.used,
                moduleType = clue.moduleType,
                currentFloorIndex = floorIndex,
                instance = instance
            };
            PropPlacer.tempPlacedClues.Add(saved);

            // Ajoute un tracker sur l'instance restaurée pour synchroniser sa sauvegarde
            var tracker = instance.GetComponent<CluePropInstance>();
            if (tracker == null) tracker = instance.AddComponent<CluePropInstance>();
            tracker.enigmaId = saved.enigmaId;
            tracker.prefabName = saved.prefabName;
            tracker.moduleType = saved.moduleType;
            tracker.used = saved.used;
            tracker.linkedSave = saved;
        }

        // Restaure les props générés par PropPlacer
        if (floorData.generatedProps != null)
        {
            foreach (var gp in floorData.generatedProps)
            {
                if (string.IsNullOrEmpty(gp.prefabName)) continue;
                GameObject prefab;
                if (!prefabByName.TryGetValue(gp.prefabName, out prefab) || prefab == null)
                {
                    // Fallback: Resources/Prefabs/<name>
                    prefab = Resources.Load<GameObject>("Prefabs/" + gp.prefabName);
                }
                if (prefab == null)
                {
                    Debug.LogWarning($"[BaseGenerator] Prefab généré introuvable: {gp.prefabName}. Skipping.");
                    continue;
                }
                var inst = Instantiate(prefab, gp.position, gp.rotation, transform);
                // Remettre le marqueur (facultatif)
                var mark = inst.GetComponent<PropPlacedInstance>() ?? inst.AddComponent<PropPlacedInstance>();
                mark.prefabName = gp.prefabName;
            }
        }

        if (floorData.consoleState != null)
        {
            var console = GameObject.FindObjectOfType<ExtractionConsoleLogic>();
            if (console != null)
            {
                console.SetValue(RotarySelector.SelectorType.Priority, floorData.consoleState.selectedPriority);
                console.SetValue(RotarySelector.SelectorType.Protocol, floorData.consoleState.selectedProtocol);
                console.SetValue(RotarySelector.SelectorType.Destination, floorData.consoleState.selectedDestination);

                foreach (var selector in console.GetComponentsInChildren<RotarySelector>())
                {
                    switch (selector.type)
                    {
                        case RotarySelector.SelectorType.Priority:
                            selector.SetIndexFromValue(floorData.consoleState.selectedPriority);
                            break;
                        case RotarySelector.SelectorType.Protocol:
                            selector.SetIndexFromValue(floorData.consoleState.selectedProtocol);
                            break;
                        case RotarySelector.SelectorType.Destination:
                            selector.SetIndexFromValue(floorData.consoleState.selectedDestination);
                            break;
                    }
                }
            }
            
            if (floorData.consoleState.consoleValidated)
            {
                console.sequenceDiodeRenderer.material = console.diodeOnMaterial;
                if (console.validateButtonScript != null)
                {
                    console.validateButtonScript.ForcePressVisualOnly();
                }


                var validatedField = typeof(ExtractionConsoleLogic)
                    .GetField("isValidated", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (validatedField != null)
                    validatedField.SetValue(console, true);
            }

        }
        if (navMeshSurface != null)
        {
            Debug.Log("[BaseGenerator] Rebake du NavMesh après chargement de l'étage.");
            navMeshSurface.BuildNavMesh();
        }

        if (floorIndex == 0 && floorData.hasMob && mobPrefab != null)
        {
            if (roomPositions.TryGetValue(Vector2.zero, out RoomData roomData))
            {
                var partGen = roomData.roomObject.GetComponent<InternalPartitionGenerator>();
                if (partGen != null)
                {
                    Vector3 mobSpawnPos = new Vector3(
                        partGen.GetCenterOfLargestSubRoom().x,
                        roomData.roomObject.transform.position.y,
                        partGen.GetCenterOfLargestSubRoom().z
                    ) + mobOffset;
                    spawnedMob = Instantiate(mobPrefab, mobSpawnPos, Quaternion.identity);

                    var mobAI = spawnedMob.GetComponent<MobAI>();
                    if (mobAI != null)
                        mobAI.player = GameObject.FindGameObjectWithTag("Player")?.transform;
                }
            }
        }

        IsFloorReady = true;
        // Réactive la génération pour les futurs étages générés
        // PropPlacer.SkipGeneration = false; // Retiré: évite une course qui dupliquait les props lors d'un chargement
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
        
        var console = GameObject.FindObjectOfType<ExtractionConsoleLogic>();
        SavedConsoleState state = null;
        
        if (console != null)
        {
            console.InitRotarySelectors(); // 🔧 Assure la synchro
            state = new SavedConsoleState
            {
                securityInserted = console.socketSecurity.IsFilled,
                navigationInserted = console.socketNavigation.IsFilled,
                selectedPriority = console.GetCurrentValue(RotarySelector.SelectorType.Priority),
                selectedProtocol = console.GetCurrentValue(RotarySelector.SelectorType.Protocol),
                selectedDestination = console.GetCurrentValue(RotarySelector.SelectorType.Destination),
                consoleValidated = console.IsValidated 
            };

        }

        foreach (var clue in PropPlacer.tempPlacedClues)
        {
            if (clue.moduleType == "Security" && console != null && console.socketSecurity.IsFilled)
                clue.used = true;
            else if (clue.moduleType == "Navigation" && console != null && console.socketNavigation.IsFilled)
                clue.used = true;
            
            
            if (clue.instance != null)
            {
                clue.position = clue.instance.transform.position;
                clue.rotation = clue.instance.transform.rotation;
            }
        }
        bool mobPresent = GameObject.FindWithTag("Mob") != null;
        
        // Sauvegarde des props générés par PropPlacer
        var generated = new List<SavedPlacedProp>();
        var trackers = transform.GetComponentsInChildren<PropPlacedInstance>(true);
        foreach (var t in trackers)
        {
            if (t == null || t.gameObject == null) continue;
            var go = t.gameObject;
            if (ElevatorPropTracker.Instance != null && ElevatorPropTracker.Instance.IsInElevator(go))
                continue; // ne pas sauver ceux dans l'ascenseur
            generated.Add(new SavedPlacedProp
            {
                prefabName = t.prefabName,
                position = go.transform.position,
                rotation = go.transform.rotation
            });
        }
        
        return new FloorSaveData
        {
            floorIndex = floorIndex,
            rooms = new List<RoomExportData>(exportedRoomData),
            placedClueProps = PropPlacer.tempPlacedClues
                .Where(c => 
                    c.instance != null &&
                    c.currentFloorIndex == floorIndex &&
                    !ElevatorPropTracker.Instance.IsInElevator(c.instance))
                .ToList(),
            consoleState = state,
            hasMob = mobPresent,
            generatedProps = generated
        };
    }

    #endregion

    #region Generation
    IEnumerator GenerateMapCoroutine(int floorIndex)
    {
        // 1. Crée la salle de départ (0,0)
        Vector2 startGridPos = Vector2.zero;
        Debug.Log($"Generating first room at grid {startGridPos}");

        GameObject startRoom = InstantiateRoom(startGridPos, floorIndex);
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

            GameObject newRoom = InstantiateRoom(roomPos, floorIndex);
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
        
        if (navMeshSurface != null)
        {
            Debug.Log("[BaseGenerator] Bake du NavMesh à la fin de la génération…");
            navMeshSurface.BuildNavMesh();
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        if (floorIndex == 0 && mobPrefab != null)
        {
            if (roomPositions.TryGetValue(Vector2.zero, out RoomData roomData))
            {
                var partGen = roomData.roomObject.GetComponent<InternalPartitionGenerator>();
                if (partGen != null)
                {
                    Vector3 mobSpawnPos = partGen.GetCenterOfLargestSubRoom() + mobOffset;
                    spawnedMob = Instantiate(mobPrefab, mobSpawnPos, Quaternion.identity);

                    var mobAI = spawnedMob.GetComponent<MobAI>();
                    if (mobAI != null)
                        mobAI.player = GameObject.FindGameObjectWithTag("Player")?.transform;
                }
            }
        }

        
        IsFloorReady = true;
        yield break;
    }
    #endregion

    #region Room utilities
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
    private GameObject InstantiateRoom(Vector2 gridPos, int floorIndex)
    {
        float half = roomSize * 0.5f;
        Vector3 offset = new Vector3(
            gridPos.x * roomSize,
            0f,
            (gridPos.y * roomSize) + half   // On ajoute half sur Z pour que le bord Sud colle à l'ascenseur
        );

        Vector3 worldPos = floorAnchor + offset;
        GameObject roomObj = Instantiate(roomPrefab, worldPos, Quaternion.identity, transform);

        foreach (var placer in roomObj.GetComponentsInChildren<PropPlacer>())
        {
            placer.currentFloor = floorIndex;
        }

        return roomObj;
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
    public void ClearOldData()
    {
        foreach (var kvp in roomPositions)
        {
            if (kvp.Value.roomObject != null)
                Destroy(kvp.Value.roomObject);
        }

        roomPositions.Clear();
        exportedRoomData.Clear();

        foreach (Transform child in transform)
        {
            if (PropTransferManager.Instance != null && 
                PropTransferManager.Instance.propsToPreserve.Contains(child.gameObject))
            {
                child.SetParent(null);
                continue;
            }

            Destroy(child.gameObject);
        }


        PropPlacer.tempPlacedClues.Clear();
    
        IsFloorReady = false;
    }


    #endregion
}

#region Data structures
[System.Serializable]
public class FloorSaveData
{
    public int floorIndex;
    public List<RoomExportData> rooms;
    public List<SavedClueProp> placedClueProps = new();
    public SavedConsoleState consoleState;
    public bool hasMob;
    public List<SavedPlacedProp> generatedProps = new();
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

[System.Serializable]
public class SavedConsoleState
{
    public bool securityInserted;
    public bool navigationInserted;
    public string selectedPriority;
    public string selectedProtocol;
    public string selectedDestination;
    public bool consoleValidated;
}


[System.Serializable]
public class SavedClueProp
{
    public string enigmaId;
    public string prefabName;
    public Vector3 position;
    public Quaternion rotation;
    public bool used; 
    public string moduleType;
    public int currentFloorIndex;
    [System.NonSerialized] public GameObject instance;
}

[System.Serializable]
public class SavedPlacedProp
{
    public string prefabName;
    public Vector3 position;
    public Quaternion rotation;
}



#endregion

