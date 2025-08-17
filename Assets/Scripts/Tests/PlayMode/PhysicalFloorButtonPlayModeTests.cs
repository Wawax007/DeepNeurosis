using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tests.PlayMode
{
    public class PhysicalFloorButtonPlayModeTests
    {
        [Test]
        public void Interact_StartsElevatorSequenceAndBlocksDoor()
        {
            // Elevator setup
            var ctrlGo = new GameObject("ElevatorController");
            var ctrl = ctrlGo.AddComponent<ElevatorController>();
            ctrl.floorManager = new GameObject("FM").AddComponent<FloorManager>();
            ctrl.audioSource = ctrlGo.AddComponent<AudioSource>();
            ctrl.doorAnimator = ctrlGo.AddComponent<Animator>();
            var blocker = new GameObject("DoorBlocker");
            blocker.SetActive(false);
            ctrl.doorBlocker = blocker;
            ctrl.doorCloseDelay = 0.05f;
            ctrl.moveDuration = 0.05f;
            ctrl.doorOpenDuration = 0.05f;

            // Button setup
            var btnGo = new GameObject("Button");
            var button = btnGo.AddComponent<PhysicalFloorButton>();
            button.elevatorController = ctrl;
            button.floorNumber = 1;

            // Act
            button.Interact();

            // Assert: coroutine a démarré → doorBlocker actif et isMoving = true
            Assert.IsTrue(blocker.activeSelf, "DoorBlocker doit être actif après Interact");
            var field = typeof(ElevatorController).GetField("isMoving", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field);
            Assert.IsTrue((bool)field.GetValue(ctrl), "L’ascenseur devrait être en mouvement après Interact");

            Object.DestroyImmediate(btnGo);
            Object.DestroyImmediate(blocker);
            Object.DestroyImmediate(ctrl.floorManager.gameObject);
            Object.DestroyImmediate(ctrlGo);
        }
    }
}

