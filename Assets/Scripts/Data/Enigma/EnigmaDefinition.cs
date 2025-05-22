using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewEnigmaDefinition", menuName = "DeepNeurosis/Enigma Definition")]
public class EnigmaDefinition : ScriptableObject
{
    public string enigmaId;

    [System.Serializable]
    public class EnigmaStep
    {
        public int floorIndex;
        public EnigmaData clueToPlace;
    }

    public EnigmaStep[] steps;

    [System.NonSerialized]
    public HashSet<string> alreadyPlacedIds = new();

    public EnigmaData[] GetCluesForFloor(int floor)
    {
        var result = new List<EnigmaData>();
        foreach (var step in steps)
        {
            if (step.floorIndex == floor && step.clueToPlace != null)
                result.Add(step.clueToPlace);
        }
        return result.ToArray();
    }

    public void ResetPlacements()
    {
        alreadyPlacedIds.Clear();
    }
}
