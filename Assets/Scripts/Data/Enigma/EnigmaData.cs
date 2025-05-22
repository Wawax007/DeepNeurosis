using UnityEngine;

public enum CluePropType { Floor, Poster, Console }

[CreateAssetMenu(fileName = "NewEnigmaData", menuName = "DeepNeurosis/Enigma Data")]
public class EnigmaData : ScriptableObject
{
    public string enigmaId;
    public GameObject cluePrefab;
    public AudioClip audioHint;
    public Sprite posterHint;
    public CluePropType propType;
}
