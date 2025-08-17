using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ExtractionConsole.Module;

// ============================================================================
//  PROPPLACER – VERSION COMPLÈTE 08‑07‑2025
//  • Pipeline "Cluster + Décorateur" + fallback anchors.
//  • Grille d’occupation partagée SpatialMap.
//  • Générateur PoissonDisk interne.
//  • Compatible avec InternalPartitionGenerator (gridRows / gridColumns publics + CellToWorld).
// ============================================================================
public class PropPlacer : MonoBehaviour
{
    public void PlaceClueProps() => PlaceAllProps();

    // ---------------------------------------------------------------------
    //   CHAMPS EXPOSES
    // ---------------------------------------------------------------------
    [Header("N° d'étage de cette salle")]
    [HideInInspector] public int currentFloor;
    
    // Empêche la génération pendant le chargement d'un étage depuis une sauvegarde
    public static bool SkipGeneration = false;

    [Header("Librairie de clusters (macro‑prefabs)")]
    public ClusterAsset[] clusterLibrary;

    [Header("Micro‑props (bric‑à‑brac)")]
    public GameObject[] microProps;

    [Header("Props accent (plantes, lampes…)")]
    public GameObject[] accentProps;

    [Header("Tags d’ancre autorisés (fallback)")]
    public string[] environmentAnchorTags = { "EnvAnchor", "FloorAnchor", "ShelfAnchor" };

    [Header("Rayon de validation des anchors")]
    public float checkRadius = 1f;

    [Header("Nombre de props via anchors legacy")]
    public int numberOfEnvironmentProps = 6;

    // ---------------------------------------------------------------------
    //   ETAT INTERNE
    // ---------------------------------------------------------------------
    private readonly List<Transform> floorAnchors = new();      // anchors d’énigme
    private readonly HashSet<Transform> usedEnvAnchors = new(); // anchors legacy déjà pris

    public static readonly List<SavedClueProp> tempPlacedClues = new();

    // =====================================================================
    //   ENTREE PRINCIPALE
    // =====================================================================
    public void PlaceAllProps()
    {
        // Bloque toute génération si on est en cours de chargement depuis une sauvegarde
        if (SkipGeneration)
        {
            Debug.Log($"[PropPlacer:{name}] Chargement en cours → génération ignorée.");
            return;
        }

        // Si la sauvegarde a déjà restauré des props → on ne génère pas.
        if (tempPlacedClues.Any(c => c.position != Vector3.zero && !c.used))
        {
            Debug.Log($"[PropPlacer:{name}] Props déjà restaurés depuis la sauvegarde – génération ignorée.");
            return;
        }

        var rng = new System.Random(currentFloor * 73856093 ^ name.GetHashCode());

        var partGen = GetComponent<InternalPartitionGenerator>();
        if (partGen == null)
        {
            Debug.LogError("[PropPlacer] InternalPartitionGenerator manquant – abort.");
            return;
        }

        int gridX = partGen.gridColumns;
        int gridZ = partGen.gridRows;
        
        var subRooms = GatherSubRooms(partGen).ToArray();
        var map      = new SpatialMap(gridX, gridZ);

        // ---- Pass 1 : Clusters ----------------------------------------------------------------
        Debug.Log($"[PropPlacer:{name}] Placement des clusters pour {subRooms.Length} sous-salles. A l'étage {currentFloor}.");
        foreach (var sub in subRooms) PlaceClusters(sub, map, rng);
        Debug.Log($"[PropPlacer:{name}] Placement des clusters terminé.");

        // ---- Pass 2 : Micro‑props décorateur --------------------------------------------------
        foreach (var sub in subRooms) Decorate(sub, map, rng);

        // ---- Legacy anchors ------------------------------------------------------------------
        if (environmentAnchorTags.Length > 0 && microProps?.Length > 0)
            PlaceEnvironmentPropsLegacy();

        // ---- Enigma props --------------------------------------------------------------------
        FindClueAnchors();
        PlaceEnigmaClue();
    }

    // =====================================================================
    //   PASS CLUSTERS
    // =====================================================================
    private void PlaceClusters(SubRoomData sub, SpatialMap map, System.Random rng)
    {
        var pg = GetComponent<InternalPartitionGenerator>();
        if (clusterLibrary == null || clusterLibrary.Length == 0) return;

        float area = sub.size.x * sub.size.y;

        bool FloorOK(ClusterAsset c) =>
            c.allowedFloors == null || c.allowedFloors.Length == 0 ||
            c.allowedFloors.Contains(currentFloor);

        /*── Candidats filtrés (tag / area / floor) ───────────────────*/
        var candidates = clusterLibrary.Where(c =>
                         FloorOK(c) &&
                         c.tag == sub.tag &&
                         area >= c.minArea && area <= c.maxArea).ToList();
        if (candidates.Count == 0) return;

        /*── Empreinte helper ─────────────────────────────────────────*/
        bool TryPlace(ClusterAsset a, int maxPer)
        {
            bool[,] mask = FootprintToBool(a);
            int w = mask.GetLength(0), h = mask.GetLength(1);
            if (w > sub.size.x || h > sub.size.y) return false;

            int placed = 0, attempts = 0;
            while (placed < maxPer && attempts < 100)
            {
                attempts++;
                int gx = rng.Next(0, sub.size.x - w + 1);
                int gz = rng.Next(0, sub.size.y - h + 1);
                int gX = sub.originCell.x + gx;
                int gZ = sub.originCell.y + gz;
                if (!map.Fits(mask, gX, gZ)) continue;

                Vector3 pos = pg.CellToWorld(gX + w * .5f, gZ + h * .5f) + a.positionOffset;
                Quaternion rot = Quaternion.Euler(a.rotationOffset);
                var go = Instantiate(a.variants[rng.Next(a.variants.Length)], pos, rot, transform);
                if (a.snapToGround) SnapToGround(go);

                // Marque pour sauvegarde
                var mark = go.GetComponent<PropPlacedInstance>() ?? go.AddComponent<PropPlacedInstance>();
                mark.prefabName = go.name.Replace("(Clone)", "").Trim();

                map.Stamp(mask, gX, gZ);
                placed++;
            }
            return placed > 0;
        }

        /*── 1) Obligatoires d’abord ─────────────────────────────────*/
        foreach (var must in candidates.Where(c => c.alwaysSpawn))
        {
            bool ok = TryPlace(must, Mathf.Max(1, must.maxPerSubRoom));
            if (!ok) Debug.LogWarning($"[Cluster] {must.name} obligatoire non posé (SR {sub.id})");
        }

        /*── 2) Optionnels : tirage pondéré jusqu’à ce qu’il n’y ait plus de place ─*/
        var optionals = candidates.Where(c => !c.alwaysSpawn).ToList();
        if (optionals.Count == 0) return;

        int safety = 50;
        while (safety-- > 0)
        {
            ClusterAsset pick = WeightedPick(optionals, rng);
            bool placed = TryPlace(pick, 1);
            if (!placed) break; // plus de place
        }
    }



    // =====================================================================
    //   PASS DECORATEUR MICRO‑PROPS
    // =====================================================================
    private void Decorate(SubRoomData sub, SpatialMap map, System.Random rng)
    {
        if (microProps == null || microProps.Length == 0) return;

        const float cell    = 0.4f; // résolution occupancy
        const float minDist = 0.6f; // rayon Poisson

        foreach (var p in PoissonDisk.Generate(sub.size, minDist, rng))
        {
            int gX = sub.originCell.x + Mathf.FloorToInt(p.x);
            int gZ = sub.originCell.y + Mathf.FloorToInt(p.y);
            if (!map.IsFree(gX, gZ)) continue;

            Vector3 worldPos = sub.origin + new Vector3(p.x + 0.5f, 0, p.y + 0.5f);
            if (IsNearForbiddenZone(worldPos)) continue;

            bool useAccent = accentProps != null && accentProps.Length > 0 &&
                             (microProps == null || microProps.Length == 0 || rng.NextDouble() < 0.2);

            GameObject prefab = useAccent
                ? accentProps[rng.Next(accentProps.Length)]
                : microProps[rng.Next(microProps.Length)];

            var go = Instantiate(prefab, worldPos + Vector3.up * 0.5f,
                Quaternion.Euler(0, rng.Next(0, 360), 0), transform);

            // Snap au sol
            SnapToGround(go);

            // Marque pour sauvegarde
            var mark = go.GetComponent<PropPlacedInstance>() ?? go.AddComponent<PropPlacedInstance>();
            mark.prefabName = prefab.name;

            // Petit correctif pivot inversé (optionnel)
            if (go.transform.lossyScale.y > go.transform.lossyScale.x * 2f)
                go.transform.Rotate(-90, 0, 0, Space.Self);

            map.Mark(gX, gZ);
        }

    }

    // =====================================================================
    //   ENIGMA PROPS   (inchangé)
    // =====================================================================
    private void FindClueAnchors()
    {
        floorAnchors.Clear();
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t.name == "ClueAnchor") floorAnchors.Add(t);
    }

    private void PlaceEnigmaClue()
    {
        var clues = RunEnigmaManager.Instance.GetCluesForFloor(currentFloor);
        if (clues == null || clues.Length == 0) { Debug.Log($"[PropPlacer:{name}] Aucun prop d’énigme."); return; }

        var enigma = RunEnigmaManager.Instance.currentEnigma;

        Dictionary<string, List<Transform>> anchorsByTag = new()
        {
            { "ConsoleAnchor", new List<Transform>() },
            { "PosterAnchor",  new List<Transform>() },
            { "FloorAnchor",   new List<Transform>() },
            { "ClueAnchor",    new List<Transform>() }
        };

        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            foreach (var tag in anchorsByTag.Keys)
                if (t.CompareTag(tag) && !IsNearForbiddenZone(t.position)) anchorsByTag[tag].Add(t);

        foreach (var clue in clues)
        {
            if (enigma.alreadyPlacedIds.Contains(clue.enigmaId)) continue;
            if (tempPlacedClues.Any(p => p.enigmaId == clue.enigmaId && p.used)) continue;

            string reqTag = clue.propType switch
            {
                CluePropType.Console => "ConsoleAnchor",
                CluePropType.Poster  => "PosterAnchor",
                _                    => "ClueAnchor"
            };

            var available = anchorsByTag[reqTag];
            Vector3 pos; Quaternion rot;

            if (clue.propType == CluePropType.Console)
            {
                if (available.Count > 0)
                {
                    var a = available[Random.Range(0, available.Count)]; available.Remove(a);
                    pos = a.position; rot = Quaternion.Euler(-90, a.rotation.eulerAngles.y, -90);
                }
                else
                {
                    pos = GetComponent<InternalPartitionGenerator>().GetCenterOfLargestSubRoom() + Vector3.up * 1.7f;
                    rot = Quaternion.Euler(-90, 0, -90);
                }
            }
            else
            {
                if (available.Count == 0) { Debug.LogWarning($"[PropPlacer] Pas d'anchor {reqTag}"); continue; }
                var a = available[Random.Range(0, available.Count)]; available.Remove(a);
                pos = a.position; rot = a.rotation;
            }

            var inst = Instantiate(clue.cluePrefab, pos, rot, transform);
            if (clue.propType == CluePropType.Floor) SnapToGround(inst);

            var saved = new SavedClueProp
            {
                enigmaId = clue.enigmaId,
                prefabName = clue.cluePrefab.name,
                position = inst.transform.position,
                rotation = inst.transform.rotation,
                used = false,
                moduleType = clue.cluePrefab.GetComponent<ModuleItem>()?.moduleType.ToString(),
                currentFloorIndex = currentFloor,
                instance = inst
            };
            tempPlacedClues.Add(saved);
            inst.gameObject.AddComponent<CluePropInstance>().linkedSave = saved;
            enigma.alreadyPlacedIds.Add(clue.enigmaId);
        }
    }

    // =====================================================================
    //   LEGACY ANCHORS – ENVIRONMENT
    // =====================================================================
    private void PlaceEnvironmentPropsLegacy()
    {
        usedEnvAnchors.Clear();
        List<Transform> anchors = FindEnvironmentAnchors();
        for (int i = 0; i < numberOfEnvironmentProps && anchors.Count > 0; i++)
        {
            Transform a = anchors.FirstOrDefault(t => !usedEnvAnchors.Contains(t));
            if (a == null) break;
            usedEnvAnchors.Add(a); anchors.Remove(a);
    
            GameObject prefab = microProps[Random.Range(0, microProps.Length)];
            var inst = Instantiate(prefab, a.position, a.rotation, transform);
    
            SnapToGround(inst);
    
            // Marque pour sauvegarde
            var mark = inst.GetComponent<PropPlacedInstance>() ?? inst.AddComponent<PropPlacedInstance>();
            mark.prefabName = prefab.name;
    
            if (inst.name.Contains("MetalTable"))
            {
                inst.transform.position += Vector3.up * 1.2f;
                inst.transform.rotation *= Quaternion.Euler(-90, 0, 0);
            }
        }
    }

    private List<Transform> FindEnvironmentAnchors()
    {
        var list = new List<Transform>();
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (environmentAnchorTags.Any(tag => t.CompareTag(tag))) list.Add(t);
        return list;
    }

    // =====================================================================
    //   HELPERS GLOBAUX
    // =====================================================================
    private bool IsNearForbiddenZone(Vector3 pos)
    {
        foreach (var hit in Physics.OverlapSphere(pos, checkRadius))
            if ((hit.GetComponent<WallTag>()?.wallType == WallType.Window) || hit.GetComponent<PropBlocker>() != null)
                return true;
        return false;
    }

    private void SnapToGround(GameObject go)
    {
        if (Physics.Raycast(go.transform.position + Vector3.up, Vector3.down, out var h, 5f)) go.transform.position = h.point;
    }

    private static ClusterAsset WeightedPick(IList<ClusterAsset> items, System.Random rng)
    {
        float total = items.Sum(i => i.weight);
        float roll  = (float)rng.NextDouble() * total;
        foreach (var i in items) { roll -= i.weight; if (roll <= 0) return i; }
        return items[0];
    }
    
    

    private static bool[,] FootprintToBool(ClusterAsset asset)
    {
        if (asset.useRectMask || asset.footprintMask == null)
        {
            int w = asset.rectWidth;
            int h = asset.rectHeight;
            bool[,] rect = new bool[w, h];
            for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                rect[x, y] = true;      // rectangle plein
            return rect;
        }
        else
        {
            return TextureToBool(asset.footprintMask);
        }
    }


    /// <summary>
    /// Convertit chaque pixel d’une Texture2D en bool.
    /// Blanc / opaque = true (cellule occupée)
    /// Noir / transparent = false (cellule libre)
    /// </summary>
    private static bool[,] TextureToBool(Texture2D tex)
    {
        int w = tex.width;
        int h = tex.height;
        bool[,] mask = new bool[w, h];

        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Color c = tex.GetPixel(x, y);
                mask[x, y] = c.a > 0.1f || c.grayscale > 0.3f; // seuils simples
            }
        }
        return mask;
    }

    private IEnumerable<SubRoomData> GatherSubRooms(InternalPartitionGenerator pg)
    {
        foreach (var sr in pg.GetAllSubRooms())
        {
            Vector3 worldOrigin = pg.CellToWorld(sr.origin.x, sr.origin.y);
            yield return new SubRoomData { id = sr.id, origin = worldOrigin, originCell = sr.origin, size = sr.size, tag = sr.tag };
        }
    }

    // =====================================================================
    //   STRUCTS / CLASSES INTERNES
    // =====================================================================
    public struct SubRoomData
    {
        public int id; public Vector3 origin; public Vector2Int originCell; public Vector2Int size; public string tag;
    }

    public class SpatialMap
    {
        private readonly bool[,] vox; public SpatialMap(int sx, int sz) => vox = new bool[sx, sz];
        public bool IsFree(int x, int z) => !vox[x, z];
        public void Mark(int x, int z)   => vox[x, z] = true;
        public bool Fits(bool[,] mask, int gx, int gz)
        {
            int w = mask.GetLength(0), h = mask.GetLength(1);
            for (int ix = 0; ix < w; ix++) for (int iz = 0; iz < h; iz++)
                if (mask[ix, iz] && !IsFree(gx + ix, gz + iz)) return false;
            return true;
        }
        public void Stamp(bool[,] mask, int gx, int gz)
        {
            int w = mask.GetLength(0), h = mask.GetLength(1);
            for (int ix = 0; ix < w; ix++) for (int iz = 0; iz < h; iz++)
                if (mask[ix, iz]) Mark(gx + ix, gz + iz);
        }
    }

    public static class PoissonDisk
    {
        public static List<Vector2> Generate(Vector2Int area, float minDist, System.Random rng, int maxAttempts = 30)
        {
            List<Vector2> pts = new();
            int target = Mathf.CeilToInt(area.x * area.y * 0.35f);
            int attempts = 0;
            while (pts.Count < target && attempts < target * maxAttempts)
            {
                attempts++;
                Vector2 p = new((float)rng.NextDouble() * area.x, (float)rng.NextDouble() * area.y);
                if (pts.All(q => (p - q).sqrMagnitude >= minDist * minDist)) pts.Add(p);
            }
            return pts;
        }
    }
}

// ========================================================================
//   CLUE PROP TRACKER (statique)
// ========================================================================
public static class CluePropTracker
{
    public static void MarkUsed(string enigmaId)
    {
        var clue = PropPlacer.tempPlacedClues.FirstOrDefault(c => c.enigmaId == enigmaId);
        if (clue != null) { clue.used = true; Debug.Log($"[CluePropTracker] Prop {enigmaId} marqué utilisé."); }
        else              { Debug.LogWarning($"[CluePropTracker] Pas de prop {enigmaId} trouvé."); }
    }
}
