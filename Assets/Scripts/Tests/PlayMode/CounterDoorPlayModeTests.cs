using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tests.PlayMode
{
    public class CounterDoorPlayModeTests
    {
        private IEnumerator RunPrivateRotate(CounterDoor door)
        {
            var mi = typeof(CounterDoor).GetMethod("RotateDoor", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "RotateDoor introuvable");
            var e = (IEnumerator)mi.Invoke(door, null);
            while (e.MoveNext()) { }
            yield break;
        }

        [Test]
        public void ForceInsertFuse_SetsDiodeOn_AndOpensDoor()
        {
            var go = new GameObject("CounterDoor");
            var door = go.AddComponent<CounterDoor>();

            // Pivot
            var pivot = new GameObject("Pivot").transform;
            pivot.SetParent(go.transform, false);
            pivot.localEulerAngles = Vector3.zero;
            door.pivot = pivot;
            door.openAngle = 90f;

            // Fuse socket
            var socket = new GameObject("Socket").transform;
            socket.SetParent(go.transform, false);
            door.fuseSocket = socket;

            // Diode renderer + materials
            var diodeGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var renderer = diodeGo.GetComponent<MeshRenderer>();
            var matOff = new Material(Shader.Find("Standard"));
            var matOn  = new Material(Shader.Find("Standard"));
            door.diodeRenderer = renderer;
            door.diodeOffMaterial = matOff;
            door.diodeOnMaterial = matOn;

            // Init (Start)
            go.SendMessage("Start");
            // Compare les shaders (Unity instancie un matériau runtime si on touche .material)
            Assert.AreEqual(matOff.shader, renderer.material.shader, "La diode doit utiliser le shader OFF au départ");

            // Act
            door.ForceInsertFuse();
            // Consomme la rotation
            var e = RunPrivateRotate(door);
            while (e.MoveNext()) { }

            // Assert diode + rotation
            Assert.AreEqual(matOn.shader, renderer.material.shader, "La diode doit utiliser le shader ON après ForceInsertFuse");
            var expected = Quaternion.Euler(0f, 0f, door.openAngle);
            Assert.Less(Quaternion.Angle(pivot.localRotation, expected), 1f, "La porte doit être ouverte");

            Object.DestroyImmediate(diodeGo);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Interact_TogglesOpenClose()
        {
            var go = new GameObject("CounterDoor");
            var door = go.AddComponent<CounterDoor>();

            var pivot = new GameObject("Pivot").transform;
            pivot.SetParent(go.transform, false);
            pivot.localEulerAngles = Vector3.zero;
            door.pivot = pivot;
            door.openAngle = 60f;

            var socket = new GameObject("Socket").transform;
            socket.SetParent(go.transform, false);
            door.fuseSocket = socket;

            var diodeGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var renderer = diodeGo.GetComponent<MeshRenderer>();
            door.diodeRenderer = renderer;
            door.diodeOffMaterial = new Material(Shader.Find("Standard"));
            door.diodeOnMaterial = new Material(Shader.Find("Standard"));

            go.SendMessage("Start");

            // Ouvre
            door.Interact();
            var e1 = RunPrivateRotate(door);
            while (e1.MoveNext()) { }
            var expectedOpen = Quaternion.Euler(0f, 0f, door.openAngle);
            Assert.Less(Quaternion.Angle(pivot.localRotation, expectedOpen), 1f);

            // Ferme
            door.Interact();
            var e2 = RunPrivateRotate(door);
            while (e2.MoveNext()) { }
            var expectedClosed = Quaternion.identity;
            Assert.Less(Quaternion.Angle(pivot.localRotation, expectedClosed), 1f);

            Object.DestroyImmediate(diodeGo);
            Object.DestroyImmediate(go);
        }
    }
}
