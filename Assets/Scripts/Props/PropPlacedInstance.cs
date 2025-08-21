using UnityEngine;

/// <summary>
/// Marque une instance placée par PropPlacer (clusters, micro-props, legacy anchors)
/// pour qu'elle puisse être sauvegardée/restaurée.
/// </summary>
public class PropPlacedInstance : MonoBehaviour
{
    /// <summary>
    /// Nom du prefab d’origine utilisé pour instancier cet objet (sans suffixe "(Clone)").
    /// </summary>
    public string prefabName;
}
