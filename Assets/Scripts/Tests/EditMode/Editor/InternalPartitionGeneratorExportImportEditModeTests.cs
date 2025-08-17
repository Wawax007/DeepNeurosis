using NUnit.Framework;
using UnityEngine;
using System.Reflection;

namespace Tests.EditMode.Editor
{
    public class InternalPartitionGeneratorExportImportEditModeTests
    {
        [Test]
        public void ExportThenImport_ReproducesPartitionsAndDoors()
        {
            var go = new GameObject("Room");
            var gen = go.AddComponent<InternalPartitionGenerator>();
            gen.gridRows = 4;
            gen.gridColumns = 4;
            gen.roomDimensions = new Vector3(20f, 0.1f, 20f);
            gen.wallPrefabsNoDoor = new[] { new GameObject("NoDoorWall") };
            gen.wallPrefabsWithDoor = new[] { new GameObject("DoorWall") };
            gen.SetSeed(9876);

            // Remplace Start(): initialisation déterministe + étapes privées
            Random.InitState(9876);
            InvokePrivate(gen, "GenerateRandomPartitions");
            InvokePrivate(gen, "DetermineSubRooms");
            InvokePrivate(gen, "PlaceNoDoorWalls");
            InvokePrivate(gen, "ReplaceOneWallWithDoor");
            InvokePrivate(gen, "EnsureAllSubRoomsHaveDoor");
            InvokePrivate(gen, "EnsureFullConnectivity");

            var saved = gen.ExportPartition();
            Assert.NotNull(saved);
            Assert.NotNull(saved.horizontalPartitions);
            Assert.NotNull(saved.verticalPartitions);

            // Réimporte sur un nouveau générateur
            var go2 = new GameObject("Room2");
            var gen2 = go2.AddComponent<InternalPartitionGenerator>();
            gen2.gridRows = gen.gridRows;
            gen2.gridColumns = gen.gridColumns;
            gen2.roomDimensions = gen.roomDimensions;
            gen2.wallPrefabsNoDoor = gen.wallPrefabsNoDoor;
            gen2.wallPrefabsWithDoor = gen.wallPrefabsWithDoor;

            gen2.ImportPartition(saved);
            var again = gen2.ExportPartition();

            Assert.AreEqual(saved.horizontalPartitions.Count, again.horizontalPartitions.Count);
            Assert.AreEqual(saved.verticalPartitions.Count, again.verticalPartitions.Count);
            Assert.AreEqual(saved.doors.Count, again.doors.Count);

            // Nettoyage
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(go2);
        }

        static void InvokePrivate(object instance, string method)
        {
            var mi = instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(mi, $"Méthode privée {method} introuvable");
            mi.Invoke(instance, null);
        }
    }
}
