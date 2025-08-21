using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ajuste dynamiquement un RectTransform pour remplir l’écran, en tenant compte du scale du Canvas.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class FitRectToScreen : MonoBehaviour
{
    RectTransform rt;
    Canvas canvas;

    void Awake()        { Cache(); }
    void OnEnable()     { Cache(); UpdateSize(); Canvas.willRenderCanvases += UpdateSize; }
    void OnDisable()    { Canvas.willRenderCanvases -= UpdateSize; }
    void OnValidate()   { Cache(); UpdateSize(); }

    void Cache()
    {
        if (!rt)     rt = (RectTransform)transform;
        if (!canvas) canvas = GetComponentInParent<Canvas>();
    }

    void UpdateSize()
    {
        if (!rt) return;
        if (!canvas) canvas = GetComponentInParent<Canvas>();

        float scale = canvas ? canvas.scaleFactor : 1f;
        float w = Screen.width  / scale;
        float h = Screen.height / scale;

        // Met juste à jour Width / Height (pas les anchors, pas la position)
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   h);
    }
}