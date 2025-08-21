using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fragment de verre sauvegardé (position et rotation) pour la vitre cassée de la StartRoom.
/// </summary>
[System.Serializable]
public class SavedGlassFragment
{
    public Vector3 position;
    public Quaternion rotation;
}

/// <summary>
/// Données de sauvegarde de la StartRoom (extincteur, compteur, état de la vitre, etc.).
/// </summary>
[System.Serializable]
public class StartRoomSaveData
{
    public Vector3 extinguisherPosition;
    public Quaternion extinguisherRotation;
    public bool counterFuseInserted;
    public bool glassBroken;
    public List<SavedGlassFragment> glassFragments = new();
}

/// <summary>
/// Cache temporaire pour collecter les fragments de verre avant sérialisation.
/// </summary>
public static class GlassFragmentCache
{
    public static List<SavedGlassFragment> fragmentsForSave = null;
}