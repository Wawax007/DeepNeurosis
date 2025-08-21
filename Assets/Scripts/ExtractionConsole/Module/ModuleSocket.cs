using UnityEngine;
using ExtractionConsole.Module;

/// <summary>
/// Socket de la console d’extraction: accepte un ModuleItem du type requis,
/// l’ancre et met à jour la diode/état.
/// </summary>
public class ModuleSocket : MonoBehaviour
{
    public ModuleType requiredType;
    public bool IsFilled { get; private set; }

    [Header("Diode visuelle")]
    public Renderer diodeRenderer;
    public Material diodeOffMaterial;
    public Material diodeOnMaterial;

    private void OnEnable()
    {
        InitDiode(); // Assure que la diode reflète l'état actuel à l'activation
    }

    /// <summary>
    /// Met à jour la diode en fonction de l'état actuel
    /// </summary>
    public void InitDiode()
    {
        if (diodeRenderer != null)
        {
            diodeRenderer.material = IsFilled ? diodeOnMaterial : diodeOffMaterial;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsFilled) return;

        ModuleItem module = other.GetComponent<ModuleItem>();
        if (module != null && module.moduleType == requiredType)
        {
            IsFilled = true;
            module.AnchorToSocket(transform.position, transform.rotation);

            if (diodeRenderer != null && diodeOnMaterial != null)
            {
                diodeRenderer.material = diodeOnMaterial;
            }

            if (!string.IsNullOrEmpty(module.enigmaId))
            {
                CluePropTracker.MarkUsed(module.enigmaId);
            }
        }
    }

    public void ForceFill(Collider other)
    {
        ModuleItem module = other.GetComponent<ModuleItem>();
        if (module != null && module.moduleType == requiredType)
        {
            IsFilled = true;
            module.AnchorToSocket(transform.position, transform.rotation);

            if (diodeRenderer != null && diodeOnMaterial != null)
            {
                diodeRenderer.material = diodeOnMaterial;
            }

            if (!string.IsNullOrEmpty(module.enigmaId))
            {
                CluePropTracker.MarkUsed(module.enigmaId);
            }
        }
    }
}