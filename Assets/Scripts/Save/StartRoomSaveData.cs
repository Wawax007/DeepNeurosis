using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SavedGlassFragment
{
    public Vector3 position;
    public Quaternion rotation;
}

[System.Serializable]
public class StartRoomSaveData
{
    public Vector3 extinguisherPosition;
    public Quaternion extinguisherRotation;
    public bool counterFuseInserted;
    public bool glassBroken;
    public List<SavedGlassFragment> glassFragments = new();
}
public static class GlassFragmentCache
{
    public static List<SavedGlassFragment> fragmentsForSave = null;
}