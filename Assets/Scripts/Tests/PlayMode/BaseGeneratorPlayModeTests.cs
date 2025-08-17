using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tests.PlayMode
{
    public class BaseGeneratorPlayModeTests
    {
        private BaseGenerator CreateGenerator(GameObject roomPrefab)
        {
            var go = new GameObject("BaseGeneratorTest");
            var gen = go.AddComponent<BaseGenerator>();
            gen.roomPrefab = roomPrefab;
            gen.numberOfRooms = 5; // valeur par défaut, sera surchargée par floorIndex -1/0/1
            return gen;
        }

        private IEnumerator RunGeneration(BaseGenerator gen, int floorIndex)
        {
            var mi = typeof(BaseGenerator).GetMethod("GenerateMapCoroutine", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "GenerateMapCoroutine non trouvé");
            var e = (IEnumerator)mi.Invoke(gen, new object[] { floorIndex });
            Assert.IsNotNull(e);
            while (e.MoveNext()) { /* avance la coroutine sans attendre le temps réel */ }
            yield break;
        }

        [Test]
        public void GeneratesCorrectRoomCount_PerFloorPreset()
        {
            var roomPrefab = new GameObject("RoomPrefab");
            var gen = CreateGenerator(roomPrefab);

            // Étages préconfigurés: -1→2, 0→3, 1→2
            int[] floors = new[] { -1, 0, 1 };
            int[] expected = new[] { 2, 3, 2 };

            for (int i = 0; i < floors.Length; i++)
            {
                // Exécute la génération de façon synchrone
                var e = RunGeneration(gen, floors[i]);
                while (e.MoveNext()) { }
                Assert.IsTrue(gen.IsFloorReady, "IsFloorReady devrait être true après génération");

                var data = gen.GetFloorData(floors[i]);
                Assert.AreEqual(expected[i], data.rooms.Count, $"Nombre de salles attendu pour l'étage {floors[i]}");

                // Reset pour le prochain tour
                gen.ClearOldData();
            }

            Object.DestroyImmediate(gen.gameObject);
            Object.DestroyImmediate(roomPrefab);
        }

        [Test]
        public void OriginRoomHasSouthOpen_AndNoRoomsBehindElevator()
        {
            var roomPrefab = new GameObject("RoomPrefab");
            var gen = CreateGenerator(roomPrefab);

            // Utilise un étage 0 (3 salles)
            var e = RunGeneration(gen, 0);
            while (e.MoveNext()) { }

            var data = gen.GetFloorData(0);
            Assert.GreaterOrEqual(data.rooms.Count, 1, "Au moins la salle d'origine devrait exister");

            // La salle d'origine (0,0) doit exister et son mur Sud (index 1) doit être ouvert (false)
            var origin = data.rooms.FirstOrDefault(r => r.position == Vector2.zero);
            Assert.IsNotNull(origin, "La salle d'origine (0,0) doit exister");
            Assert.IsFalse(origin.walls[1], "Le mur Sud de (0,0) doit être ouvert (connecté à l'ascenseur)");

            // Aucune salle ne doit avoir une coordonnée y négative (pas de construction derrière l'ascenseur)
            Assert.IsTrue(data.rooms.All(r => r.position.y >= 0f), "Aucune salle ne doit être générée avec y<0");

            // Nettoyage
            gen.ClearOldData();
            Object.DestroyImmediate(gen.gameObject);
            Object.DestroyImmediate(roomPrefab);
        }

        [Test]
        public void ClearOldData_ResetsStateAndRooms()
        {
            var roomPrefab = new GameObject("RoomPrefab");
            var gen = CreateGenerator(roomPrefab);

            var e = RunGeneration(gen, 1); // 2 salles
            while (e.MoveNext()) { }
            Assert.IsTrue(gen.IsFloorReady);

            gen.ClearOldData();
            Assert.IsFalse(gen.IsFloorReady, "IsFloorReady doit repasser à false");
            var data = gen.GetFloorData(1);
            Assert.AreEqual(0, data.rooms.Count, "Après ClearOldData, aucune salle ne doit rester");

            Object.DestroyImmediate(gen.gameObject);
            Object.DestroyImmediate(roomPrefab);
        }

        [Test]
        public void DefaultFloor_ClampsNumberOfRoomsToMax100()
        {
            var roomPrefab = new GameObject("RoomPrefab");
            var gen = CreateGenerator(roomPrefab);
            gen.numberOfRooms = 150; // au-dessus de la limite

            var e = RunGeneration(gen, 5); // étage par défaut
            while (e.MoveNext()) { }

            var data = gen.GetFloorData(5);
            Assert.AreEqual(100, data.rooms.Count, "Le nombre de salles doit être clampé à 100 pour un étage par défaut");

            gen.ClearOldData();
            Object.DestroyImmediate(gen.gameObject);
            Object.DestroyImmediate(roomPrefab);
        }
    }
}
