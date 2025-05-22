using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class OutlineMeshCreator : MonoBehaviour
{
    [Tooltip("Matériau utilisé pour dessiner l'outline (couleur unie, cull=Front).")]
    public Material outlineMaterial;

    [Tooltip("Facteur d'échelle pour gonfler le mesh dupliqué (ex: 1.02).")]
    public float outlineScale = 1.02f;

    private GameObject outlineChild;

    private void Start()
    {
        CreateOutlineChild();
        SetHighlight(false); // désactivé par défaut
    }

    private void CreateOutlineChild()
    {
        if (outlineMaterial == null)
        {
            Debug.LogWarning($"[OutlineMeshCreator] Pas de matériau Outline assigné sur {name}.");
            return;
        }

        // 1) Crée un GameObject enfant
        outlineChild = new GameObject("OutlineMesh");
        outlineChild.transform.SetParent(transform);
        outlineChild.transform.localPosition = Vector3.zero;
        outlineChild.transform.localRotation = Quaternion.identity;
        outlineChild.transform.localScale = Vector3.one * outlineScale;

        // 2) Copie le mesh
        MeshFilter parentMF = GetComponent<MeshFilter>();
        MeshRenderer parentMR = GetComponent<MeshRenderer>();

        if (parentMF != null && parentMF.sharedMesh != null)
        {
            MeshFilter childMF = outlineChild.AddComponent<MeshFilter>();
            childMF.sharedMesh = parentMF.sharedMesh;

            // 3) Le renderer de l'enfant avec le matériau outline
            MeshRenderer childMR = outlineChild.AddComponent<MeshRenderer>();
            childMR.sharedMaterial = outlineMaterial;

            // Optionnel : recopier d'autres params depuis parentMR (shadowCasting, etc.)
            childMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            childMR.receiveShadows = false;
        }
    }

    public void SetHighlight(bool active)
    {
        if (outlineChild != null)
        {
            outlineChild.SetActive(active);
        }
    }
}
