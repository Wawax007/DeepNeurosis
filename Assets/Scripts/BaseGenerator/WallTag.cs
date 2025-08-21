using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WallType { Normal, Window, Forbidden }

/// <summary>
/// Tag simple apposé sur les segments de mur pour indiquer leur type (plein, fenêtre, interdit).
/// </summary>
public class WallTag : MonoBehaviour
{
    public WallType wallType = WallType.Normal;
}
