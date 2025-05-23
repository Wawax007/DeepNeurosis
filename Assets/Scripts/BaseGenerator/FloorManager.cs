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
    
    private void Start()
    {
        string dirPath = Path.Combine(Application.persistentDataPath, saveFolderName);
        if (!Directory.Exists(dirPath))
            Directory.CreateDirectory(dirPath);

        startRoomObj = GameObject.Find("StartRoom");
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

            Debug.Log("[FloorManager] StartRoom already in scene. No generation or loading required.");
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



    private void SaveAndUnloadCurrentFloor()
    {
        if (currentFloor == -2)
        {
            SetStartRoomActive(false); 
            return;
        }

        SaveFloorToJson(currentFloor);
        // Note: la destruction des salles générées est gérée par BaseGenerator lorsqu’on regénère.
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

    
    private void SaveFloorToJson(int floorIndex)
    {
        if (floorIndex == -2)
        {
            Debug.Log("[FloorManager] StartRoom does not need to be saved.");
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
