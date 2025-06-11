using System;
using ExtractionConsole.Module;
using UnityEngine;
using UnityEngine.Events;

public class ExtractionConsoleLogic : MonoBehaviour
{
    public ModuleSocket socketSecurity;
    public ModuleSocket socketNavigation;

    public enum Priority { Navigation, Security }
    public enum Protocol { Manual, Auto }
    public enum Destination { Surface, EmergencyNode }

    [Header("Sélections actuelles")]
    public Priority selectedPriority = Priority.Navigation;
    public Protocol selectedProtocol = Protocol.Manual;
    public Destination selectedDestination = Destination.Surface;

    [Header("Résultat")]
    public UnityEvent onSuccess;
    public UnityEvent onFailure;

    [Header("Diode de validation")]
    public Renderer sequenceDiodeRenderer;
    public Material diodeOffMaterial;
    public Material diodeOnMaterial;

    [Header("Feedback Audio")]
    public AudioSource audioSource;
    public AudioClip successSound;
    public AudioClip errorBuzzer;

    private string correctCode = "Security-Auto-EmergencyNode";
    private bool isValidated = false;
    public bool IsValidated => isValidated; 

    [Header("Bouton d’activation")]
    public Transform validateButton; // assigné dans l’inspector

    private Vector3 buttonPressedPosition;
    private Vector3 buttonReleasedPosition;
    
    [Header("Référence bouton")]
    public ValidateButton validateButtonScript;
    
    
    private void Awake()
    {
        // Initialise les valeurs même si elles sont déjà dans les champs pour éviter les doutes
        selectedPriority = Priority.Navigation;
        selectedProtocol = Protocol.Manual;
        selectedDestination = Destination.Surface;
    }

    public bool AreModulesInserted()
    {
        bool areInserted = socketSecurity != null && socketSecurity.IsFilled &&
                           socketNavigation != null && socketNavigation.IsFilled;

        if (areInserted)
        {
            PlaySound(successSound);
        }

        return areInserted;
    }

    public void SetValue(RotarySelector.SelectorType type, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            Debug.LogWarning($"[ExtractionConsoleLogic] '{type}' a une valeur vide ou nulle. Ignoré.");
            return;
        }

        try
        {
            switch (type)
            {
                case RotarySelector.SelectorType.Priority:
                    selectedPriority = (Priority)Enum.Parse(typeof(Priority), value, true);
                    break;
                case RotarySelector.SelectorType.Protocol:
                    selectedProtocol = (Protocol)Enum.Parse(typeof(Protocol), value, true);
                    break;
                case RotarySelector.SelectorType.Destination:
                    selectedDestination = (Destination)Enum.Parse(typeof(Destination), value, true);
                    break;
            }

            Debug.Log($"[ExtractionConsoleLogic] {type} = {value} défini avec succès.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ExtractionConsoleLogic] Erreur lors du SetValue({type}, \"{value}\") : {e.Message}");
        }
    }

    public void ResetSelectors()
    {
        foreach (var selector in FindObjectsOfType<RotarySelector>())
        {
            selector.ResetToDefault();
        }
    }

    public bool Validate()
    {
        if (!AreModulesInserted())
        {
            Debug.Log("Modules non insérés.");
            PlaySound(errorBuzzer);
            return false;
        }

        string currentCombo = $"{selectedPriority}-{selectedProtocol}-{selectedDestination}";
        bool isCorrect = (currentCombo == correctCode);

        if (sequenceDiodeRenderer != null)
            sequenceDiodeRenderer.material = isCorrect ? diodeOnMaterial : diodeOffMaterial;

        if (isCorrect)
        {
            var capsuleActivator = GameObject.FindObjectOfType<ExtractionPodActivator>();
            if (capsuleActivator != null)
            {
                capsuleActivator.EnableCapsule();
            }
            isValidated = true;

            Debug.Log("Séquence correcte. Extraction débloquée.");
            PlaySound(successSound);
            onSuccess?.Invoke();
        }

        else
        {
            Debug.Log("Séquence incorrecte.");
            PlaySound(errorBuzzer);
            onFailure?.Invoke();
        }

        return isCorrect;
    }
    
    public void InitRotarySelectors()
    {
        foreach (var selector in GetComponentsInChildren<RotarySelector>())
        {
            selector.ApplyCurrentValue(); // à créer
        }
    }
    
    public string GetCurrentValue(RotarySelector.SelectorType type)
    {
        switch (type)
        {
            case RotarySelector.SelectorType.Priority:
                return selectedPriority.ToString();
            case RotarySelector.SelectorType.Protocol:
                return selectedProtocol.ToString();
            case RotarySelector.SelectorType.Destination:
                return selectedDestination.ToString();
            default:
                return "Unknown";
        }
    }

    public void GetCurrentSelectorValues(out string priority, out string protocol, out string destination)
    {
        priority = selectedPriority.ToString();
        protocol = selectedProtocol.ToString();
        destination = selectedDestination.ToString();
    }


    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}
