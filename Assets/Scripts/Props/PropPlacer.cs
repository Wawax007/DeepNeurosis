    using UnityEngine;
    using System.Collections.Generic;
    using System.Linq;
    using ExtractionConsole.Module;

    public class PropPlacer : MonoBehaviour
    {
        [Header("N° d'étage de cette salle")]
        public int floorIndex;

        [Header("Props environnementaux à disperser")]
        public GameObject[] environmentProps;

        [Header("Tags d’ancre autorisés pour props environnementaux")]
        public string[] environmentAnchorTags = { "EnvAnchor", "TableAnchor", "ShelfAnchor" };

        [Header("Rayon de validation des anchors")]
        public float checkRadius = 1f;

        private List<Transform> floorAnchors = new List<Transform>();

        public static List<SavedClueProp> tempPlacedClues = new();

        public void PlaceClueProps()
        {
            if (PropPlacer.tempPlacedClues.Any(c => c.position != Vector3.zero && c.used == false))
            {
                Debug.Log($"[PropPlacer:{name}] Des props sont déjà chargés depuis la sauvegarde. Ignoré.");
                return;
            }

            
            FindAnchors();

            PlaceEnigmaClue();
            PlaceEnvironmentProps();
        }

        private void FindAnchors()
        {
            floorAnchors.Clear();
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
                if (t.name == "FloorAnchor") floorAnchors.Add(t);
        }

        private void PlaceEnigmaClue()
        {
            var clues = RunEnigmaManager.Instance.GetCluesForFloor(floorIndex);
            if (clues == null || clues.Length == 0)
            {
                Debug.Log($"[PropPlacer:{name}] Aucun prop d’énigme à placer pour l’étage {floorIndex}.");
                return;
            }

            var enigma = RunEnigmaManager.Instance.currentEnigma;

            Dictionary<string, List<Transform>> anchorsByTag = new()
            {
                { "ConsoleAnchor", new List<Transform>() },
                { "PosterAnchor", new List<Transform>() },
                { "FloorAnchor", new List<Transform>() }
            };

            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                foreach (var tag in anchorsByTag.Keys)
                {
                    if (t.CompareTag(tag) && !IsNearForbiddenZone(t.position))
                        anchorsByTag[tag].Add(t);
                }
            }

            foreach (var clue in clues)
            {
                if (enigma.alreadyPlacedIds.Contains(clue.enigmaId)) continue;
                if (tempPlacedClues.Any(p => p.enigmaId == clue.enigmaId && p.used)) continue;

                string requiredTag = clue.propType switch
                {
                    CluePropType.Console => "ConsoleAnchor",
                    CluePropType.Poster  => "PosterAnchor",
                    _                    => "FloorAnchor"
                };

                var availableAnchors = anchorsByTag[requiredTag];

                Vector3 position;
                Quaternion rotation;

                if (clue.propType == CluePropType.Console)
                {
                    if (availableAnchors.Count > 0)
                    {
                        int i = Random.Range(0, availableAnchors.Count);
                        var anchor = availableAnchors[i];
                        availableAnchors.RemoveAt(i);

                        position = anchor.position;
                        rotation = Quaternion.Euler(-90, anchor.rotation.eulerAngles.y, -90);
                    }
                    else
                    {
                        var partGen = GetComponent<InternalPartitionGenerator>();
                        position = new Vector3(partGen.GetCenterOfLargestSubRoom().x, 1.7f, partGen.GetCenterOfLargestSubRoom().z);
                        rotation = Quaternion.Euler(-90, 0, -90);
                        Debug.LogWarning($"[PropPlacer:{name}] Aucun anchor '{requiredTag}' valide pour {clue.enigmaId}, fallback au centre.");
                    }
                }
                else
                {
                    if (availableAnchors.Count > 0)
                    {
                        int i = Random.Range(0, availableAnchors.Count);
                        var anchor = availableAnchors[i];
                        availableAnchors.RemoveAt(i);

                        position = anchor.position;
                         rotation = anchor.rotation;
                    }
                    else
                    {
                        Debug.LogWarning($"[PropPlacer:{name}] Aucun anchor '{requiredTag}' valide pour {clue.enigmaId}");
                        continue;
                    }
                }

                var instance = Instantiate(clue.cluePrefab, position, rotation, transform);
                if (clue.propType == CluePropType.Floor)
                    SnapToGround(instance);

                string moduleTypeStr = null;
                var moduleComp = clue.cluePrefab.GetComponent<ModuleItem>();
                if (moduleComp != null)
                    moduleTypeStr = moduleComp.moduleType.ToString();   

                var saved = new SavedClueProp
                {
                    enigmaId = clue.enigmaId,
                    prefabName = clue.cluePrefab.name,
                    position = instance.transform.position,
                    rotation = instance.transform.rotation,
                    used = false,
                    moduleType = moduleTypeStr,
                    currentFloorIndex = floorIndex,
                    instance = instance
                };

                tempPlacedClues.Add(saved);

                var tracker = instance.AddComponent<CluePropInstance>();
                tracker.linkedSave = saved;

                
                enigma.alreadyPlacedIds.Add(clue.enigmaId);
                Debug.Log($"[PropPlacer:{name}] Prop {clue.enigmaId} placé.");
            }
        }

        private void PlaceEnvironmentProps()
        {
            List<Transform> envAnchors = FindEnvironmentAnchors();
            foreach (var prop in environmentProps)
            {
                if (envAnchors.Count == 0) break;
                int index = Random.Range(0, envAnchors.Count);
                Transform anchor = envAnchors[index];
                envAnchors.RemoveAt(index);
                Instantiate(prop, anchor.position, anchor.rotation, transform);
            }
        }

        private List<Transform> FindEnvironmentAnchors()
        {
            List<Transform> anchors = new List<Transform>();
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                foreach (var tag in environmentAnchorTags)
                {
                    if (t.CompareTag(tag)) anchors.Add(t);
                }
            }
            return anchors;
        }

        private Transform GetValidAnchor(List<Transform> anchors)
        {
            foreach (var anchor in anchors)
            {
                if (!IsNearForbiddenZone(anchor.position))
                    return anchor;
            }
            return null;
        }

        private bool IsNearForbiddenZone(Vector3 pos)
        {
            Collider[] hits = Physics.OverlapSphere(pos, checkRadius);
            foreach (var hit in hits)
            {
                WallTag wall = hit.GetComponent<WallTag>();
                if (wall != null && wall.wallType == WallType.Window) return true;
                if (hit.GetComponent<PropBlocker>() != null) return true;
            }
            return false;
        }

        private void SnapToGround(GameObject obj)
        {
            RaycastHit hit;
            Vector3 startPos = obj.transform.position + Vector3.up * 1f;
            if (Physics.Raycast(startPos, Vector3.down, out hit, 5f))
                obj.transform.position = hit.point;
            else
                Debug.LogWarning($"[{name}] Aucun sol détecté sous {obj.name}, position conservée.");
        }
    }

    public static class CluePropTracker
    {
        public static void MarkUsed(string enigmaId)
        {
            var clue = PropPlacer.tempPlacedClues.FirstOrDefault(c => c.enigmaId == enigmaId);
            if (clue != null)
            {
                clue.used = true;
                Debug.Log($"[CluePropTracker] Prop {enigmaId} marqué comme utilisé.");
            }
            else
            {
                Debug.LogWarning($"[CluePropTracker] Aucun prop trouvé avec enigmaId = {enigmaId} à marquer comme utilisé.");
            }
        }
    }
