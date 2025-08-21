using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExtractionConsole.Module
{
    /// <summary>
    /// Représente un module insérable (Security/Navigation) et gère son ancrage dans un socket.
    /// </summary>
    public class ModuleItem : MonoBehaviour
    {
        public ModuleType moduleType;
        public string enigmaId;
        public bool IsAnchored { get; private set; }

        public void AnchorToSocket(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            IsAnchored = true;
        }
    }

}