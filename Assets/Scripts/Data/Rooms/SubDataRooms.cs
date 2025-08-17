using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SubRoomData
{
    public int id;
    public Vector3 origin;       // en unités monde (coin bas‑gauche)
    public Vector2Int originCell;// même chose mais grille
    public Vector2Int size;      // largeur, hauteur (en cellules)
    public string tag;           // "bureau", "storage"…
}
