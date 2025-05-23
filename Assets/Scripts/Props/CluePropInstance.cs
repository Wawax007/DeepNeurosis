using UnityEngine;

/// <summary>
/// Permet de suivre une instance de prop d'énigme dans la scène.
/// Sert à la sauvegarde/restauration.
/// </summary>
public class CluePropInstance : MonoBehaviour
{
    public string enigmaId;
    public string prefabName;
    public string moduleType;
    public bool used;
    public SavedClueProp linkedSave;
    
    
    private void LateUpdate()
    {
        if (linkedSave != null)
        {
            linkedSave.position = transform.position;
            linkedSave.rotation = transform.rotation;
        }
    }   
}