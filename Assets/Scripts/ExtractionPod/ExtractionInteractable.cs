using UnityEngine;

/// <summary>
/// Déclenche l’enchaînement d’extraction lorsqu’interagi, en s’assurant de ne le faire qu’une fois.
/// </summary>
public class ExtractionInteractable : MonoBehaviour, IInteractable
{
    private bool hasBeenUsed = false;

    public ExtractionPodActivator podActivator;

    public void Interact()
    {
        if (hasBeenUsed || podActivator == null) return;

        hasBeenUsed = true;
        podActivator.StartExtractionSequence();
    }
}