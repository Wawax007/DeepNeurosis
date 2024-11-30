using UnityEngine;
using UnityEngine.UI;

public class SetupRenderTexture : MonoBehaviour
{
    public Camera renderCamera;  // Ta caméra utilisant une RenderTexture
    public RawImage displayUI;  // L'objet RawImage dans l'UI

    void Start()
    {
        if (renderCamera.targetTexture != null)
        {
            displayUI.texture = renderCamera.targetTexture;
        }
        else
        {
            Debug.LogError("La caméra n'a pas de RenderTexture assignée !");
        }
    }
}