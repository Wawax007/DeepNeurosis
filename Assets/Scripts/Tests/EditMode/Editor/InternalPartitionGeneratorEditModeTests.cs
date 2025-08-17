using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode.Editor
{
    public class InternalPartitionGeneratorEditModeTests
    {
        [Test]
        public void Start_WithSeed_PopulatesSubRoomsAndCenters()
        {
            var go = new GameObject("Room");
            var gen = go.AddComponent<InternalPartitionGenerator>();
            gen.gridRows = 4;
            gen.gridColumns = 4;
            gen.roomDimensions = new Vector3(40f, 0.1f, 40f);
            gen.wallPrefabsNoDoor = new GameObject[0];
            gen.wallPrefabsWithDoor = new GameObject[0];
            gen.SetSeed(1234);

            // Remplace Start(): initialisation déterministe + appel des étapes privées
            Random.InitState(1234);
            InvokePrivate(gen, "GenerateRandomPartitions");
            InvokePrivate(gen, "DetermineSubRooms");
            InvokePrivate(gen, "PlaceNoDoorWalls");
            InvokePrivate(gen, "ReplaceOneWallWithDoor");
            InvokePrivate(gen, "EnsureAllSubRoomsHaveDoor");
            InvokePrivate(gen, "EnsureFullConnectivity");

            var subRooms = gen.GetAllSubRooms().ToList();
            var centers = gen.GetCentersOfAllRooms();
            var patrol = gen.GetAllPatrolPoints();

            Assert.That(subRooms.Count, Is.GreaterThan(0));
            Assert.That(centers.Count, Is.GreaterThan(0));
            Assert.That(patrol.Count, Is.GreaterThan(0));
            Assert.AreEqual(centers.Count, patrol.Count);

            float halfX = gen.roomDimensions.x * 0.5f + 0.01f;
            float halfZ = gen.roomDimensions.z * 0.5f + 0.01f;
            foreach (var c in centers)
            {
                var local = go.transform.InverseTransformPoint(c);
                Assert.That(local.x, Is.InRange(-halfX, halfX));
                Assert.That(local.z, Is.InRange(-halfZ, halfZ));
            }

            Object.DestroyImmediate(go);
        }

        [Test]
        public void CellToWorld_IncreasesWithRowAndColumn()
        {
            var go = new GameObject("Room");
            var gen = go.AddComponent<InternalPartitionGenerator>();
            gen.gridRows = 4;
            gen.gridColumns = 4;
            gen.roomDimensions = new Vector3(40f, 0.1f, 40f);

            Vector3 a = gen.CellToWorld(0, 0);
            Vector3 b = gen.CellToWorld(1, 1);

            Assert.Greater(b.x, a.x);
            Assert.Greater(b.z, a.z);
            Object.DestroyImmediate(go);
        }

        static void InvokePrivate(object instance, string method)
        {
            var mi = instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(mi, $"Méthode privée {method} introuvable");
            mi.Invoke(instance, null);
        }
    }
}
