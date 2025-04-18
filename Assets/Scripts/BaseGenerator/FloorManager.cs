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

    private void Start()
    {
        // Crée le dossier de sauvegarde si besoin
        string dirPath = Path.Combine(Application.persistentDataPath, saveFolderName);
        if (!Directory.Exists(dirPath))
            Directory.CreateDirectory(dirPath);

        // Ici, la Start Room (-2) est déjà dans la scène, manuelle.
        // On n'appelle pas GenerateFloor(-2) ni LoadFloorFromJson(-2).
        // Le joueur est donc dans cette Start Room au démarrage.
    }

    /// <summary>
    /// Appelé quand le joueur choisit un étage sur l'ascenseur.
    /// </summary>
    public void GoToFloor(int targetFloor)
    {
        // 1) Sauvegarder/détruire l’étage actuel
        SaveAndUnloadCurrentFloor();

        // 2) Mettre à jour l’étage courant
        currentFloor = targetFloor;

        // 3) Vérifier si un fichier de sauvegarde existe pour cet étage
        if (FloorSaveExists(currentFloor))
        {
            // Charger l’étage depuis JSON
            LoadFloorFromJson(currentFloor);
        }
        else
        {
            // Générer l’étage
            baseGenerator.GenerateFloor(currentFloor);
        }

        // 4) Attendre que tout soit prêt avant de laisser le joueur s’y rendre
        //    (pas de téléportation, on attend juste la génération pour éviter d’y entrer trop tôt)
        StartCoroutine(WaitForFloorReady());
    }

    private void SaveAndUnloadCurrentFloor()
    {
        // Si c'est l'étage -2 (Start Room), on ne la sauvegarde pas (puisqu'elle est manuelle)
        // Mais on veut la détruire pour la remplacer par un autre étage plus tard.
        if (currentFloor == -2)
        {
            // Retrouve l'objet "StartRoom" dans la scène et le supprime
            var startRoomObj = GameObject.Find("StartRoom");
            if (startRoomObj != null)
            {
                Destroy(startRoomObj);
                Debug.Log("[FloorManager] Start Room destroyed.");
            }
        }
        else
        {
            // Pour les autres étages, on les sauvegarde
            SaveFloorToJson(currentFloor);
        }
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

    private void SaveFloorToJson(int floorIndex)
    {
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
