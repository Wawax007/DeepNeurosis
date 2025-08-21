using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExtractionConsole.Module;
using UnityEngine;

/// <summary>
/// Gère le transfert et la préservation des props entre étages via l’ascenseur.
/// Assure le détachement, la re‑parentalisation dans la room la plus proche
/// et la mise à jour des données de sauvegarde associées (CluePropInstance).
/// </summary>
public class PropTransferManager : MonoBehaviour
{
    public static PropTransferManager Instance;

    public HashSet<GameObject> propsToPreserve = new();

    private void Awake() => Instance = this;

    public void MarkPropsInElevatorForPreservation()
    {
        propsToPreserve.Clear();

        foreach (var prop in ElevatorPropTracker.Instance.trackedProps)
        {
            if (prop != null)
            {
                propsToPreserve.Add(prop);

                if (prop.transform.parent != null)
                    prop.transform.SetParent(null);

                var clue = prop.GetComponent<CluePropInstance>();
                if (clue != null)
                {
                    clue.linkedSave.currentFloorIndex = -999;
                }
            }
        }
    }

    private Transform FindNearestRoomParent(Vector3 propPosition, Transform floorRoot)
    {
        float minDistance = float.MaxValue;
        Transform nearestRoom = null;

        foreach (Transform child in floorRoot)
        {
            if (child.name.Contains("Room"))
            {
                float dist = Vector3.Distance(propPosition, child.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestRoom = child;
                }
            }
        }

        return nearestRoom;
    }

    public void OnPropExitedElevator(GameObject prop)
    {
        var clue = prop.GetComponent<CluePropInstance>();
        if (clue == null) return;

        var baseGen = FindObjectOfType<BaseGenerator>();
        if (baseGen == null) return;

        // Trouver la room la plus proche
        var closestRoom = baseGen.GetComponentsInChildren<Transform>()
            .Where(t => t.name.Contains("SubMarineBase") || t.name.Contains("StartRoom"))
            .OrderBy(t => Vector3.Distance(t.position, prop.transform.position))
            .FirstOrDefault();

        if (closestRoom != null)
        {
            prop.transform.SetParent(closestRoom);
            clue.linkedSave.position = prop.transform.position;
            clue.linkedSave.rotation = prop.transform.rotation;
            clue.linkedSave.instance = prop;
            clue.linkedSave.currentFloorIndex = baseGen.GetFloorIndex();

            if (!PropPlacer.tempPlacedClues.Contains(clue.linkedSave))
            {
                if (string.IsNullOrEmpty(clue.linkedSave.enigmaId))
                    clue.linkedSave.enigmaId = clue.gameObject.name;

                if (string.IsNullOrEmpty(clue.linkedSave.prefabName))
                    clue.linkedSave.prefabName = clue.gameObject.name.Replace("(Clone)", "").Trim();

                if (string.IsNullOrEmpty(clue.linkedSave.moduleType))
                {
                    var module = clue.GetComponent<ModuleItem>();
                    if (module != null)
                        clue.linkedSave.moduleType = module.moduleType.ToString();
                }

                PropPlacer.tempPlacedClues.Add(clue.linkedSave);
            }

            Debug.Log($"[PropTransfer] Prop {prop.name} transféré vers étage {clue.linkedSave.currentFloorIndex} et attaché à {closestRoom.name}");
        }
        else
        {
            Debug.LogWarning("[PropTransfer] Aucune Room trouvée pour rattacher le prop.");
        }
    }



}
