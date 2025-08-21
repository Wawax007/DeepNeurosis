using UnityEngine;

/// <summary>
/// Orchestrateur d’énigmes pour une run : sélection de l’énigme active,
/// accès aux indices (clues) par étage et réinitialisation des placements.
/// </summary>
public class RunEnigmaManager : MonoBehaviour
{
    public static RunEnigmaManager Instance;

    [Header("Toutes les énigmes disponibles")]
    public EnigmaDefinition[] allEnigmas;

    [Header("Énigme active")]
    public EnigmaDefinition currentEnigma;

    public int currentRun = 1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ChooseEnigmaForRun()
    {
        if (currentRun == 1)
            currentEnigma = allEnigmas[0];
        else if (currentRun == 2)
            currentEnigma = allEnigmas[1];
        else
            currentEnigma = allEnigmas[Random.Range(0, allEnigmas.Length)];

        currentEnigma.ResetPlacements(); // important
        Debug.Log($"[RunEnigmaManager] Énigme choisie : {currentEnigma.enigmaId}");
    }


    public EnigmaData[] GetCluesForFloor(int floorIndex)
    {
        if (currentEnigma == null) return null;
        return currentEnigma.GetCluesForFloor(floorIndex);
    }
}