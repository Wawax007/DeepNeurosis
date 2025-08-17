using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace Tests.PlayMode
{
    public class ElevatorPlayModeTests
    {
        private ElevatorController CreateElevator(out FloorManager fm, float doorClose=0.05f, float move=0.05f, float open=0.05f)
        {
            var root = new GameObject("ElevatorRoot");

            // FloorManager + BaseGenerator minimal
            var fmGo = new GameObject("FloorManager");
            fm = fmGo.AddComponent<FloorManager>();
            var baseGenGo = new GameObject("BaseGenerator");
            var baseGen = baseGenGo.AddComponent<BaseGenerator>();
            fm.baseGenerator = baseGen;

            // Controller
            var ctrlGo = new GameObject("ElevatorController");
            var ctrl = ctrlGo.AddComponent<ElevatorController>();
            ctrl.floorManager = fm;
            ctrl.doorCloseDelay = doorClose;
            ctrl.moveDuration  = move;
            ctrl.doorOpenDuration = open;

            // Door blocker
            var blocker = new GameObject("DoorBlocker");
            blocker.SetActive(false); // commence désactivé pour un test pertinent
            ctrl.doorBlocker = blocker;

            // Optional: Animator & AudioSource (not required for logic)
            ctrl.doorAnimator = ctrlGo.AddComponent<Animator>();
            ctrl.audioSource = ctrlGo.AddComponent<AudioSource>();

            // Parent for tidy hierarchy
            fmGo.transform.SetParent(root.transform);
            baseGenGo.transform.SetParent(root.transform);
            ctrlGo.transform.SetParent(root.transform);
            blocker.transform.SetParent(root.transform);

            return ctrl;
        }

        [Test]
        public void ButtonTriggersSequence_GoToExtractionPod_Synchronous()
        {
            var ctrl = CreateElevator(out var fm, 0.02f, 0.03f, 0.02f);

            // Récupère la coroutine privée et l'itère étape par étape
            var mi = typeof(ElevatorController).GetMethod("HandleFloorTransition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(mi, "HandleFloorTransition method not found");
            var enumerator = (IEnumerator)mi.Invoke(ctrl, new object[] { 2 });
            Assert.IsNotNull(enumerator);

            // Première étape: fermeture des portes → doorBlocker actif, attente doorCloseDelay
            Assert.IsTrue(enumerator.MoveNext(), "Coroutine should yield doorClose delay");
            Assert.IsTrue(ctrl.doorBlocker.activeSelf, "Door should be blocked at first yield");

            // Deuxième étape: début du mouvement → appel GoToFloor, attente moveDuration
            Assert.IsTrue(enumerator.MoveNext(), "Coroutine should yield move duration");
            Assert.AreEqual(2, fm.currentFloor, "FloorManager should target floor 2 after second step");

            // Troisième étape: ding + ouverture → attente doorOpenDuration
            Assert.IsTrue(enumerator.MoveNext(), "Coroutine should yield door open duration");

            // Dernière étape: fin → doorBlocker inactif, isMoving faux
            Assert.IsFalse(enumerator.MoveNext(), "Coroutine should complete");
            Assert.IsFalse(ctrl.doorBlocker.activeSelf, "Door should be unblocked at the end");
        }

        [Test]
        public void IgnoreButtonWhileAlreadyMoving()
        {
            var ctrl = CreateElevator(out var fm, 0.05f, 0.05f, 0.02f);

            // Simule un état en mouvement pour ignorer l'appui
            var field = typeof(ElevatorController).GetField("isMoving", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(field, "isMoving field not found");
            field.SetValue(ctrl, true);

            ctrl.OnFloorButtonPressed(0);

            // Aucune transition ne doit être déclenchée
            Assert.AreNotEqual(0, fm.currentFloor, "Floor should not change while moving");
        }
    }
}
