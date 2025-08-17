using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode.Editor
{
    public class ElevatorPropTrackerEditModeTests
    {
        [Test]
        public void Instance_IsSetAndIsInElevatorReflectsTrackedSet()
        {
            var go = new GameObject("ElevatorTracker");
            var tracker = go.AddComponent<ElevatorPropTracker>();

            // En EditMode, éviter d’invoquer Awake/Start manuellement; on assigne Instance directement
            ElevatorPropTracker.Instance = tracker;
            Assert.AreSame(tracker, ElevatorPropTracker.Instance, "Instance statique doit référencer le tracker");

            var prop = new GameObject("Prop");
            Assert.IsFalse(tracker.IsInElevator(prop));

            tracker.trackedProps.Add(prop);
            Assert.IsTrue(tracker.IsInElevator(prop));

            tracker.trackedProps.Remove(prop);
            Assert.IsFalse(tracker.IsInElevator(prop));

            // Cleanup
            ElevatorPropTracker.Instance = null;
            Object.DestroyImmediate(prop);
            Object.DestroyImmediate(go);
        }
    }
}
