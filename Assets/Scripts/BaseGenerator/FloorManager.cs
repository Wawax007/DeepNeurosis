using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

/// <summary>
/// Gère la persistance et le chargement des étages (StartRoom, étages procéduraux, ExtractionPod).
/// </summary>
public class FloorManager : MonoBehaviour
{
    [Header("References")]
    /// <summary>
    /// Générateur principal utilisé pour (re)générer un étage.
    /// </summary>
    public BaseGenerator baseGenerator; // Glisser l’objet qui a BaseGenerator dans l’inspecteur

    [Header("Floor Settings")]
    /// <summary>
    /// Indice de l’étage courant (-2 = StartRoom, 2 = ExtractionPod).
    /// </summary>
    public int currentFloor = -2;       
    /// <summary>
    /// Nom du dossier persistant où sont écrites/lu les données des étages.
    /// </summary>
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

    private void OnApplicationQuit()
    {
        // Sauvegarde automatique selon l’étage courant
        if (currentFloor == -2)
        {
            SaveStartRoom();
        }
        else if (currentFloor != 2)
        {
            SaveFloorToJson(currentFloor);
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (!pause) return;
        // Sauvegarde de précaution en cas de mise en pause (ex: alt-tab, suspend)
        if (currentFloor == -2)
        {
            SaveStartRoom();
        }
        else if (currentFloor != 2)
        {
            SaveFloorToJson(currentFloor);
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
    /// <param name="targetFloor">Indice de l’étage à charger (-2 StartRoom, 2 ExtractionPod).</param>
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

            // Protéger contre extractionPodObj non assigné en scène/tests
            if (extractionPodObj != null && HasAnyConsoleBeenValidated())
            {
                var activator = extractionPodObj.GetComponentInChildren<ExtractionPodActivator>();
                if (activator != null)
                {
                    activator.ApplyInteractionLayerIfConsoleValidated(true);
                    activator.PlayExtractionMusic();
                }
            }
            else if (extractionPodObj == null)
            {
                Debug.LogWarning("[FloorManager] extractionPodObj non assigné; activation simple sans configuration d'ExtractionPodActivator.");
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
            // Recherche de tous les conteneurs de verre cassé (compat: BrokenGlass / breakedGlass)
            foreach (Transform child in glassParent)
            {
                string n = child.name.ToLowerInvariant();
                if (n.Contains("brokenglass") || n.Contains("breakedglass"))
                {
                    var rbs = child.GetComponentsInChildren<Rigidbody>();
                    foreach (var rb in rbs)
                    {
                        var t = rb.transform;
                        fragments.Add(new SavedGlassFragment
                        {
                            position = t.position,
                            rotation = t.rotation
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
            glassBroken = (glass == null) || fragments.Count > 0,
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
        if (extinguisher == null)
        {
            // Fallback: tag or partial name within StartRoom
            var byTag = GameObject.FindGameObjectWithTag("Extinguisher");
            if (byTag != null) extinguisher = byTag;
            else
            {
                var parent = GameObject.Find("StartRoom");
                if (parent != null)
                {
                    foreach (Transform t in parent.transform.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name.ToLowerInvariant().Contains("extinguisher"))
                        {
                            extinguisher = t.gameObject; break;
                        }
                    }
                }
            }
        }
        if (extinguisher != null)
        {
            var rb = extinguisher.GetComponent<Rigidbody>();
            bool hadRb = rb != null;
            if (hadRb)
            {
                rb.isKinematic = true; // gèle la physique le temps de replacer
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();
            }
            extinguisher.transform.position = data.extinguisherPosition;
            extinguisher.transform.rotation = data.extinguisherRotation;
            Physics.SyncTransforms();
            if (hadRb)
            {
                StartCoroutine(UnfreezeNextFrame(rb));
            }
        }

        CounterDoor counter = GameObject.FindObjectOfType<CounterDoor>();
        if (counter != null && data.counterFuseInserted)
            counter.ForceInsertFuse();

        if (data.glassBroken && data.glassFragments != null && data.glassFragments.Count > 0)
        {
            BreakableGlass glass = GameObject.FindObjectOfType<BreakableGlass>();
            if (glass != null)
            {
                Transform parent = GameObject.Find("StartRoom")?.transform;
                if (parent == null) return;

                // Nettoie d’éventuels restes
                foreach (Transform child in parent)
                {
                    string n = child.name.ToLowerInvariant();
                    if (n.Contains("brokenglass") || n.Contains("breakedglass"))
                        Destroy(child.gameObject);
                }

                GameObject broken = Instantiate(
                    glass.brokenGlassPrefab,
                    glass.transform.position,
                    Quaternion.Euler(0, 90, 0),
                    parent
                );

                // Restaure sur les fragments physiques
                var frags = broken.GetComponentsInChildren<Rigidbody>();
                int fragmentCount = Mathf.Min(frags.Length, data.glassFragments.Count);

                for (int i = 0; i < fragmentCount; i++)
                {
                    Transform frag = frags[i].transform;
                    frag.position = data.glassFragments[i].position;
                    frag.rotation = data.glassFragments[i].rotation;

                    var rb = frags[i];
                    rb.isKinematic = true;
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                glass.gameObject.SetActive(false);
            }
        }


    }

    private IEnumerator UnfreezeNextFrame(Rigidbody rb)
    {
        yield return null; // attend une frame
        if (rb != null)
        {
            rb.WakeUp();
            rb.isKinematic = false;
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
        GameObject mob = GameObject.FindWithTag("Mob");
        if (mob != null)
        {
            Destroy(mob);
            Debug.Log("[FloorManager] Mob détruit lors du déchargement de l'étage.");
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
