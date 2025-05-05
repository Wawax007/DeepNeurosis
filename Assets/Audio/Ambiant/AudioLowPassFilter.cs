using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioLowPassFilter : MonoBehaviour
{
    public AudioLowPassFilter lowPassFilter;
    public float cutoffFrequency = 22000; // Normal
    public float lowPassFrequency = 800; // Étouffé

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            lowPassFilter.cutoffFrequency = 800; // étouffé
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            lowPassFilter.cutoffFrequency = 22000; // normal
        }
    }

}
