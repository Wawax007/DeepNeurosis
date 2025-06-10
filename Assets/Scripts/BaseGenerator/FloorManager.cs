using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class FloorManager : MonoBehaviour
{
    [Header("References")]
    public BaseGenerator baseGenerator; // Glisser l’objet qui a BaseGenerator dans l’inspecteur

    [Header("Floor Settings")]
    public int currentFloor = -2;       
    public string saveFolderName = "FloorsData";
    private GameObject startRoomObj;
    [SerializeField] private GameObject extractionPodObj;
    
    private void Start()
    {
        string dirPath = Path.Combine(Application.persistentDataPath, saveFolderName);
        if (!Directory.Exists(dirPath))
            Directory.CreateDirectory(dirPath);

        startRoomObj = GameObject.Find("StartRoom");
        if (currentFloor == -2)
        {
            StartCoroutine(DeferredLoadStartRoom());
        }

    }
    
    private IEnumerator DeferredLoadStartRoom()
    {
        yield return null;
        LoadStartRoom();
    }


    /// <summary>
    /// Appelé quand le joueur choisit un étage sur l'ascenseur.
    /// </summary>
    public void GoToFloor(int targetFloor)
    {
        SaveAndUnloadCurrentFloor();
        currentFloor = targetFloor;

        // FLOOR -2 → StartRoom
        if (currentFloor == -2)
        {
            baseGenerator.ClearOldData();
            baseGenerator.IsFloorReady = true;

            SetStartRoomActive(true); 
            LoadStartRoom();

            Debug.Log("[FloorManager] StartRoom already in scene. No generation or loading required.");
            StartCoroutine(WaitForFloorReady());
            return;
        }
        
        // FLOOR 2 → ExtractionPod
        if (currentFloor == 2)
        {
            baseGenerator.ClearOldData();
            baseGenerator.IsFloorReady = true;

            SetExtractionPodActive(true);

            if (HasAnyConsoleBeenValidated())
            {
                var activator = extractionPodObj.GetComponentInChildren<ExtractionPodActivator>();
                if (activator != null)
                {
                    activator.ApplyInteractionLayerIfConsoleValidated(true);
                    activator.PlayExtractionMusic();
                }
            }

            Debug.Log("[FloorManager] ExtractionPod activé.");
            StartCoroutine(WaitForFloorReady());
            return;
        }



        // AUTRES ÉTAGES
        if (FloorSaveExists(currentFloor))
        {
            LoadFloorFromJson(currentFloor);
        }
        else
        {
            if (RunEnigmaManager.Instance != null)
            {
                if (RunEnigmaManager.Instance.currentEnigma == null)
                    RunEnigmaManager.Instance.ChooseEnigmaForRun();
            }
            else
            {
                Debug.LogError("[FloorManager] RunEnigmaManager.Instance is null. Cannot choose enigma for the run.");
            }

            baseGenerator.GenerateFloor(currentFloor);
        }

        StartCoroutine(WaitForFloorReady());
    }
    
    private string GetStartRoomSavePath()
    {
        return Path.Combine(Application.persistentDataPath, saveFolderName, "startRoom.json");
    }

    private void SaveStartRoom()
    {
        GameObject extinguisher = GameObject.Find("Extinguisher");
        CounterDoor counter = GameObject.FindObjectOfType<CounterDoor>();
        BreakableGlass glass = GameObject.FindObjectOfType<BreakableGlass>();

        if (extinguisher == null || counter == null) return;

        List<SavedGlassFragment> fragments = new List<SavedGlassFragment>();
        Transform glassParent = GameObject.Find("StartRoom")?.transform;

        if (glassParent != null)
        {
            foreach (Transform child in glassParent)
            {
                if (child.name.Contains("breakedGlass"))
                {
                    foreach (Transform frag in child)
                    {
                        fragments.Add(new SavedGlassFragment
                        {
                            position = frag.position,
                            rotation = frag.rotation
                        });
                    }
                }
            }
        }

        StartRoomSaveData data = new StartRoomSaveData
        {
            extinguisherPosition = extinguisher.transform.position,
            extinguisherRotation = extinguisher.transform.rotation,
            counterFuseInserted = counter.IsFuseInserted(),
            glassBroken = (glass == null),
            glassFragments = fragments
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetStartRoomSavePath(), json);

        Debug.Log("[FloorManager] StartRoom saved with " + fragments.Count + " glass fragments.");
    }


    private void LoadStartRoom()
    {
        string path = GetStartRoomSavePath();
        if (!File.Exists(path)) return;

        string json = File.ReadAllText(path);
        StartRoomSaveData data = JsonUtility.FromJson<StartRoomSaveData>(json);

        GameObject extinguisher = GameObject.Find("Extinguisher");
        if (extinguisher != null)
            extinguisher.transform.position = data.extinguisherPosition;
            extinguisher.transform.rotation = data.extinguisherRotation;
        CounterDoor counter = GameObject.FindObjectOfType<CounterDoor>();
        if (counter != null && data.counterFuseInserted)
            counter.ForceInsertFuse();

        if (data.glassBroken && data.glassFragments != null && data.glassFragments.Count > 0)
        {
            BreakableGlass glass = GameObject.FindObjectOfType<BreakableGlass>();
            if (glass != null)
            {
                Transform parent = GameObject.Find("StartRoom")?.transform;

                foreach (Transform child in parent)
                {
                    if (child.name.Contains("BrokenGlass"))
                        Destroy(child.gameObject);
                }

                GameObject broken = Instantiate(
                    glass.brokenGlassPrefab,
                    glass.transform.position,
                    Quaternion.Euler(0, 90, 0),
                    parent
                );

                Transform[] children = broken.GetComponentsInChildren<Transform>();
                int fragmentCount = Mathf.Min(children.Length, data.glassFragments.Count);

                for (int i = 0; i < fragmentCount; i++)
                {
                    Transform frag = children[i];
                    frag.position = data.glassFragments[i].position;
                    frag.rotation = data.glassFragments[i].rotation;

                    Rigidbody rb = frag.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = true; 
                    }
                }

                glass.gameObject.SetActive(false);
            }
        }


    }


    
    private bool HasAnyConsoleBeenValidated()
    {
        string[] saveFiles = Directory.GetFiles(
            Path.Combine(Application.persistentDataPath, saveFolderName),
            "floor_*.json"
        );

        foreach (var path in saveFiles)
        {
            string json = File.ReadAllText(path);
            FloorSaveData data = JsonUtility.FromJson<FloorSaveData>(json);

            if (data.consoleState != null && data.consoleState.consoleValidated)
            {
                return true;
            }
        }

        return false;
    }




    private void SaveAndUnloadCurrentFloor()
    {
        if (currentFloor == -2)
        {
            SaveStartRoom();
            SetStartRoomActive(false); 
            return;
        }
        
        if (currentFloor == 2)
        {
            SetExtractionPodActive(false); 
            return;
        }

        SaveFloorToJson(currentFloor);
    }

    private IEnumerator WaitForFloorReady()
    {
        // On attend que le générateur indique que l'étage est prêt
        while (!baseGenerator.IsFloorReady)
            yield return null;

        // ICI, on ne déplace plus le joueur, l’ascenseur est fixe, on le laisse là.
        Debug.Log("[FloorManager] Floor ready. No teleportation is performed.");
    }

    private void SetStartRoomActive(bool active)
    {
        if (startRoomObj == null) return;

        startRoomObj.SetActive(active);

        Rigidbody[] rigidbodies = startRoomObj.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rigidbodies)
        {
            if (ElevatorPropTracker.Instance != null && ElevatorPropTracker.Instance.IsInElevator(rb.gameObject))
            {
                continue;
            }

            rb.isKinematic = !active;
            rb.detectCollisions = active;
        }
    }
    
    private void SetExtractionPodActive(bool active)
    {
        if (extractionPodObj == null) return;

        extractionPodObj.SetActive(active);

        Rigidbody[] rigidbodies = extractionPodObj.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rigidbodies)
        {
            if (ElevatorPropTracker.Instance != null && ElevatorPropTracker.Instance.IsInElevator(rb.gameObject))
            {
                continue;
            }

            rb.isKinematic = !active;
            rb.detectCollisions = active;
        }
    }

    
    private void SaveFloorToJson(int floorIndex)
    {
        if (floorIndex == -2)
        {
            Debug.Log("[FloorManager] StartRoom does not need to be saved.");
            return;
        }
        if (floorIndex == 2)
        {
            Debug.Log("[FloorManager] ExtractionRoom does not need to be saved.");
            return;
        }
        // Récupère les données du baseGenerator
        FloorSaveData data = baseGenerator.GetFloorData(floorIndex);

        // Convertit en JSON
        string json = JsonUtility.ToJson(data, true);

        // Enregistre dans FloorsData/floor_X.json
        string path = GetFloorSavePath(floorIndex);
        File.WriteAllText(path, json);

        Debug.Log($"[FloorManager] Floor {floorIndex} saved to {path}");
    }

    private void LoadFloorFromJson(int floorIndex)
    {
        string path = GetFloorSavePath(floorIndex);
        if (!File.Exists(path))
        {
            Debug.LogError($"[FloorManager] No save file found for floor {floorIndex}");
            return;
        }

        // Lit le fichier et désérialise
        string json = File.ReadAllText(path);
        FloorSaveData data = JsonUtility.FromJson<FloorSaveData>(json);

        // Construit l'étage dans la scène
        baseGenerator.LoadFloorFromData(data);
    }

    private bool FloorSaveExists(int floorIndex)
    {
        return File.Exists(GetFloorSavePath(floorIndex));
    }

    private string GetFloorSavePath(int floorIndex)
    {
        // Ex: /.../FloorsData/floor_0.json
        return Path.Combine(Application.persistentDataPath, saveFolderName, $"floor_{floorIndex}.json");
    }
}
