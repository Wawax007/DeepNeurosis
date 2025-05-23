using UnityEngine;

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