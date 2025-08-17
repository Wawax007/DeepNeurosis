using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using PlayerScripts;

namespace Tests.EditMode.Editor
{
    public class FirstPersonControllerEditModeTests
    {
        private GameObject MakePlayer(out FirstPersonController fpc)
        {
            var go = new GameObject("Player_FPC_Test");
            // Ajoute explicitement le CharacterController pour éviter les soucis d'édition
            go.AddComponent<CharacterController>();
            fpc = go.AddComponent<FirstPersonController>();
            var cam = new GameObject("Camera").transform;
            cam.SetParent(go.transform, false);
            fpc.cameraTransform = cam;
            return go;
        }

        [Test]
        public void Hide_TogglesIsHidden()
        {
            var go = MakePlayer(out var fpc);

            Assert.IsFalse(fpc.isHidden);
            fpc.Hide(true);
            Assert.IsTrue(fpc.isHidden);
            fpc.ExitHide();
            Assert.IsFalse(fpc.isHidden);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SetCameraLock_TogglesPrivateFlag()
        {
            var go = MakePlayer(out var fpc);
            var field = typeof(FirstPersonController).GetField("isCursorLocked", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field);

            fpc.SetCameraLock(true);  // isCursorLocked = !true = false
            Assert.IsFalse((bool)field.GetValue(fpc));

            fpc.SetCameraLock(false); // isCursorLocked = !false = true
            Assert.IsTrue((bool)field.GetValue(fpc));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void AddingFPC_WithCharacterController_Works()
        {
            var go = new GameObject("Player_FPC_Add");
            go.AddComponent<CharacterController>();
            var fpc = go.AddComponent<FirstPersonController>();

            Assert.IsNotNull(fpc);
            Assert.IsNotNull(go.GetComponent<CharacterController>());

            Object.DestroyImmediate(go);
        }
    }
}

