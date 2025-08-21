using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ExtractionConsole.Module;

/// <summary>
/// Place les props de la salle (clusters, micro-props, accents et éléments d’énigme),
/// avec support de restauration depuis sauvegarde et d’anchors legacy.
/// </summary>
public class PropPlacer : MonoBehaviour
{
    /// <summary>
    /// Lance la génération/pose de tous les props (alias de <see cref="PlaceAllProps"/>).
    /// </summary>
    public void PlaceClueProps() => PlaceAllProps();

    // ---------------------------------------------------------------------
    //   CHAMPS EXPOSES
    // ---------------------------------------------------------------------
    [Header("N° d'étage de cette salle")]
    [HideInInspector]
    public int currentFloor;
    
    /// <summary>
    /// Si vrai, empêche toute génération (utilisé lors d’un chargement depuis une sauvegarde).
    /// </summary>
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

    /// <summary>
    /// Tampon runtime des props d’énigme placés pour la sauvegarde et le rechargement.
    /// </summary>
    public static readonly List<SavedClueProp> tempPlacedClues = new();

    // =====================================================================
    //   ENTREE PRINCIPALE
    // =====================================================================
    /// <summary>
    /// Place les différents types de props dans la salle (clusters, micro‑props,
    /// anchors legacy et éléments d’énigme) si la génération n’est pas bloquée.
    /// </summary>
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
    /// <summary>
    /// Tente de placer des clusters dans une sous-salle en respectant les contraintes
    /// de taille, de tag, d’étage et d’emprise sur la carte d’occupation.
    /// </summary>
    /// <param name="sub">Sous-salle cible.</param>
    /// <param name="map">Carte d’occupation globale pour éviter les chevauchements.</param>
    /// <param name="rng">Générateur pseudo‑aléatoire déterministe.</param>
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
    /// <summary>
    /// Éparpille des micro‑props (ou accents) dans la sous‑salle via un échantillonnage Poisson,
    /// en respectant la carte d’occupation et les zones interdites.
    /// </summary>
    /// <param name="sub">Sous-salle cible.</param>
    /// <param name="map">Carte d’occupation globale.</param>
    /// <param name="rng">Générateur pseudo‑aléatoire.</param>
    private void Decorate(SubRoomData sub, SpatialMap map, System.Random rng)
    {
        if (microProps == null || microProps.Length == 0) return;

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
    /// <summary>
    /// Recherche tous les anchors d’indices ("ClueAnchor") présents dans la hiérarchie de la salle.
    /// </summary>
    private void FindClueAnchors()
    {
        floorAnchors.Clear();
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t.name == "ClueAnchor") floorAnchors.Add(t);
    }

    /// <summary>
    /// Place les props liés à l’énigme courante sur des anchors compatibles
    /// (ou en fallback au centre de la plus grande sous‑salle) et enregistre
    /// leur état pour la sauvegarde.
    /// </summary>
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
            foreach (var anchorTag in anchorsByTag.Keys)
                if (t.CompareTag(anchorTag) && !IsNearForbiddenZone(t.position)) anchorsByTag[anchorTag].Add(t);

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
    /// <summary>
    /// Place un nombre limité de micro‑props sur des anchors « environnement » legacy
    /// identifiés par leurs tags.
    /// </summary>
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

    /// <summary>
    /// Retourne la liste des transforms marqués par l’un des tags d’anchors environnement.
    /// </summary>
    private List<Transform> FindEnvironmentAnchors()
    {
        var list = new List<Transform>();
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (environmentAnchorTags.Any(anchorTag => t.CompareTag(anchorTag))) list.Add(t);
        return list;
    }

    // =====================================================================
    //   HELPERS GLOBAUX
    // =====================================================================
    /// <summary>
    /// Indique si une position se trouve à proximité d’une zone interdite (fenêtre, bloqueur).
    /// </summary>
    /// <param name="pos">Position monde à tester.</param>
    /// <returns>Vrai si la zone est interdite pour le placement.</returns>
    private bool IsNearForbiddenZone(Vector3 pos)
    {
        foreach (var hit in Physics.OverlapSphere(pos, checkRadius))
            if ((hit.GetComponent<WallTag>()?.wallType == WallType.Window) || hit.GetComponent<PropBlocker>() != null)
                return true;
        return false;
    }

    /// <summary>
    /// Replace verticalement un objet sur le sol en projetant un rayon vers le bas.
    /// </summary>
    /// <param name="go">Objet à aligner sur le sol.</param>
    private void SnapToGround(GameObject go)
    {
        if (Physics.Raycast(go.transform.position + Vector3.up, Vector3.down, out var h, 5f)) go.transform.position = h.point;
    }

    /// <summary>
    /// Sélectionne un élément selon un poids relatif.
    /// </summary>
    /// <param name="items">Liste pondérée d’items.</param>
    /// <param name="rng">Générateur pseudo‑aléatoire.</param>
    /// <returns>Un élément de la liste, proportionnel à son poids.</returns>
    private static ClusterAsset WeightedPick(IList<ClusterAsset> items, System.Random rng)
    {
        float total = items.Sum(i => i.weight);
        float roll  = (float)rng.NextDouble() * total;
        foreach (var i in items) { roll -= i.weight; if (roll <= 0) return i; }
        return items[0];
    }
    
    

    /// <summary>
    /// Construit un masque booléen d’emprise à partir d’un <see cref="ClusterAsset"/>.
    /// Utilise un rectangle plein si le masque texture n’est pas défini.
    /// </summary>
    /// <param name="asset">Définition du cluster.</param>
    /// <returns>Masque 2D où true indique une cellule occupée.</returns>
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

    /// <summary>
    /// Agrège les sous‑salles du générateur de partitions en données prêtes à consommer.
    /// </summary>
    /// <param name="pg">Générateur de partitions internes.</param>
    /// <returns>Suite des sous‑salles (origine monde, taille, tag…).</returns>
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
    /// <summary>
    /// Données d’une sous-salle du générateur (origine, taille et tag).
    /// </summary>
    public struct SubRoomData
    {
        public int id; public Vector3 origin; public Vector2Int originCell; public Vector2Int size; public string tag;
    }

    /// <summary>
    /// Carte d’occupation de cellules pour éviter les collisions lors du placement.
    /// </summary>
    public class SpatialMap
    {
        private readonly bool[,] vox; public SpatialMap(int sx, int sz) => vox = new bool[sx, sz];
        /// <summary>
        /// Indique si la cellule (x,z) est libre.
        /// </summary>
        public bool IsFree(int x, int z) => !vox[x, z];
        /// <summary>
        /// Marque la cellule (x,z) comme occupée.
        /// </summary>
        public void Mark(int x, int z)   => vox[x, z] = true;
        /// <summary>
        /// Vérifie si un masque peut être posé à partir de (gx,gz) sans chevauchement.
        /// </summary>
        public bool Fits(bool[,] mask, int gx, int gz)
        {
            int w = mask.GetLength(0), h = mask.GetLength(1);
            for (int ix = 0; ix < w; ix++) for (int iz = 0; iz < h; iz++)
                if (mask[ix, iz] && !IsFree(gx + ix, gz + iz)) return false;
            return true;
        }
        /// <summary>
        /// Applique (occupe) un masque à partir de (gx,gz).
        /// </summary>
        public void Stamp(bool[,] mask, int gx, int gz)
        {
            int w = mask.GetLength(0), h = mask.GetLength(1);
            for (int ix = 0; ix < w; ix++) for (int iz = 0; iz < h; iz++)
                if (mask[ix, iz]) Mark(gx + ix, gz + iz);
        }
    }

    /// <summary>
    /// Générateur de points PoissonDisk (2D) pour un échantillonnage dispersé.
    /// </summary>
    public static class PoissonDisk
    {
        /// <summary>
        /// Génère des points espacés d’au moins <paramref name="minDist"/> dans une zone.
        /// </summary>
        /// <param name="area">Dimensions de la zone (en cellules).</param>
        /// <param name="minDist">Distance minimale entre deux points.</param>
        /// <param name="rng">Générateur pseudo-aléatoire.</param>
        /// <param name="maxAttempts">Nombre maximum d’essais avant arrêt.</param>
        /// <returns>Liste de positions 2D en coordonnées de cellule.</returns>
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

/// <summary>
/// Utilitaires pour marquer les props d’énigme comme « utilisés ».
/// </summary>
public static class CluePropTracker
{
    /// <summary>
    /// Marque comme utilisé le prop lié à l’énigme d’identifiant donné.
    /// </summary>
    /// <param name="enigmaId">Identifiant unique de l’énigme.</param>
    public static void MarkUsed(string enigmaId)
    {
        var clue = PropPlacer.tempPlacedClues.FirstOrDefault(c => c.enigmaId == enigmaId);
        if (clue != null) { clue.used = true; Debug.Log($"[CluePropTracker] Prop {enigmaId} marqué utilisé."); }
        else              { Debug.LogWarning($"[CluePropTracker] Pas de prop {enigmaId} trouvé."); }
    }
}
