using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Données d’une sous-salle: origine monde, indices de grille, taille en cellules et tag.
/// </summary>
public struct SubRoomData
{
    public int id;
    public Vector3 origin;       // en unités monde (coin bas‑gauche)
    public Vector2Int originCell;// même chose mais grille
    public Vector2Int size;      // largeur, hauteur (en cellules)
    public string tag;           // "bureau", "storage"…
}
