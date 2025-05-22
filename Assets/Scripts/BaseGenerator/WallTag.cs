using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WallType { Normal, Window, Forbidden }
public class WallTag : MonoBehaviour
{
    public WallType wallType = WallType.Normal;
}
