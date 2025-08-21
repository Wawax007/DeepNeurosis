using System;
using UnityEngine;

/// <summary>
/// Sélecteur rotatif de la console: gère les options (Priorité/Protocole/Destination),
/// applique la valeur à la console et met à jour la tête visuelle.
/// </summary>
public class RotarySelector : MonoBehaviour
{
    public enum SelectorType { Priority, Protocol, Destination }
    public SelectorType type;

    [Header("Tête rotative")]
    public Transform head;

    [Header("Référence à la console")]
    public ExtractionConsoleLogic consoleLogic;

    private int currentIndex = 0;
    private string[] options;

    public AudioClip rotationSound;

    private void Start()
    {
        InitOptions();

        // Force l'application de la valeur par défaut si aucune valeur restaurée n’a été appliquée
        ApplyCurrentValue();
        UpdateVisual();
    }

    private void InitOptions()
    {
        switch (type)
        {
            case SelectorType.Priority:
                options = new[] { "Navigation", "Security" };
                break;
            case SelectorType.Protocol:
                options = new[] { "Manual", "Auto" };
                break;
            case SelectorType.Destination:
                options = new[] { "Surface", "EmergencyNode" };
                break;
        }
    }

    public void SetIndexFromValue(string value)
    {
        InitOptions(); // utile si jamais Start() n'est pas encore passé

        if (options == null) return;

        for (int i = 0; i < options.Length; i++)
        {
            if (options[i].Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                currentIndex = i;
                ApplyCurrentValue();
                UpdateVisual();
                Debug.Log($"[RotarySelector:{type}] Set index to {i} from value '{value}'");
                return;
            }
        }

        Debug.LogWarning($"[RotarySelector:{type}] Value '{value}' not found in options.");
    }

    public void ApplyCurrentValue()
    {
        if (options == null || options.Length == 0 || consoleLogic == null) return;
        consoleLogic.SetValue(type, options[currentIndex]);
    }

    public void ResetToDefault()
    {
        currentIndex = 0;
        ApplyCurrentValue();
        UpdateVisual();
    }

    public void Rotate()
    {
        if (consoleLogic == null || !consoleLogic.AreModulesInserted() || consoleLogic.IsValidated) return;

        currentIndex = (currentIndex + 1) % options.Length;
        ApplyCurrentValue();
        UpdateVisual();

        if (rotationSound != null && consoleLogic.audioSource != null)
            consoleLogic.audioSource.PlayOneShot(rotationSound);

        Debug.Log($"[RotarySelector:{type}] → {options[currentIndex]}");
    }

    private void UpdateVisual()
    {
        if (head != null)
        {
            float anglePerStep = -60f;
            float targetAngle = currentIndex * anglePerStep;
            head.localRotation = Quaternion.Euler(-90, targetAngle, -160);
        }
    }
}
