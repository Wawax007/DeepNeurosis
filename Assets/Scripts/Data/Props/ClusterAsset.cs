using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Décrit un « cluster » de props (variants, empreinte/mask, contraintes de placement,
/// pondération et options d’ajustement) utilisé par le générateur de salle.
/// </summary>
[CreateAssetMenu(menuName="ProcGen/Cluster")]
public class ClusterAsset : ScriptableObject
{
    public string tag;

    [Header("Placement conditionnel")]
    [Tooltip("Laissez vide ⇒ autorisé partout")]
    public int[] allowedFloors;

    [Header("Footprint")]
    public bool   useRectMask = true;
    public int    rectWidth  = 2;
    public int    rectHeight = 3;
    public Texture2D footprintMask;

    [Header("Prefab(s)")]
    public GameObject[] variants;

    [Header("Balancing")]
    [Range(0,2f)] public float weight   = 1f;
    public float minArea = 4;
    public float maxArea = 25;
    
    [Header("Corrections (optionnel)")]
    public Vector3 positionOffset = Vector3.zero;
    public Vector3 rotationOffset = Vector3.zero;
    public bool    snapToGround   = true;  
    
    [Header("Spawn obligatoire / multiplicité")]
    public bool  alwaysSpawn      = false;
    public int   maxPerSubRoom    = 3;     
    
    [Header("Placement le long des murs")]
    public bool  placeAlongWall   = false; 
    [Range(0, 1)] public float wallProbability = 1f;
}
