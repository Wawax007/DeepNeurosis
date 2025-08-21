using UnityEngine;
using System.Collections;

/// <summary>
/// Bouton de validation de la console: anime l’appui et déclenche la validation de séquence.
/// </summary>
public class ValidateButton : MonoBehaviour
{
    public ExtractionConsoleLogic consoleLogic;

    [Header("Tête visuelle du bouton")]
    public Transform head;

    [Header("Animation")]
    public float pressDistance = 0.16f;
    public float pressDuration = 0.1f;

    private Vector3 originalLocalPos;

    private void Start()
    {
        if (head != null)
            originalLocalPos = head.localPosition;
    }

    public void Press()
    {
        if (consoleLogic == null || head == null || consoleLogic.IsValidated) return;

        bool isCorrect = consoleLogic.Validate();
        StartCoroutine(SmoothPressAnimation(isCorrect));

        if (!isCorrect)
        {
            consoleLogic.ResetSelectors();
        }
    }

    
    public void ForcePressVisualOnly()
    {
        if (head == null) return;
        Vector3 pressedLocalPos = originalLocalPos + new Vector3(0f, -pressDistance, 0f);
        head.localPosition = pressedLocalPos;
    }


    private IEnumerator SmoothPressAnimation(bool keepPressed)
    {
        Vector3 pressedLocalPos = originalLocalPos + new Vector3(0f, -pressDistance, 0f);
        float elapsed = 0f;

        while (elapsed < pressDuration)
        {
            head.localPosition = Vector3.Lerp(originalLocalPos, pressedLocalPos, elapsed / pressDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        head.localPosition = pressedLocalPos;

        if (!keepPressed)
        {
            yield return new WaitForSeconds(0.05f);

            elapsed = 0f;
            while (elapsed < pressDuration)
            {
                head.localPosition = Vector3.Lerp(pressedLocalPos, originalLocalPos, elapsed / pressDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            head.localPosition = originalLocalPos;
        }
    }
}