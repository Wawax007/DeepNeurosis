using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tests.PlayMode
{
    public class RoomGeneratorPlayModeTests
    {
        private RoomGenerator CreateRoomWithAnchors()
        {
            var go = new GameObject("Room");
            var rg = go.AddComponent<RoomGenerator>();
            rg.wallPrefab = new GameObject("Wall");
            rg.wallWithDoorPrefab = new GameObject("WallDoor");
            rg.wallWithWindowPrefab = new GameObject("WallWindow");

            new GameObject("Emplacement_N").transform.SetParent(go.transform, false);
            new GameObject("Emplacement_S").transform.SetParent(go.transform, false);
            new GameObject("Emplacement_E").transform.SetParent(go.transform, false);
            new GameObject("Emplacement_W").transform.SetParent(go.transform, false);
            return rg;
        }

        private void RunPrivateInit(RoomGenerator rg)
        {
            var mi = typeof(RoomGenerator).GetMethod("InitializeRoom", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "InitializeRoom introuvable");
            var e = (IEnumerator)mi.Invoke(rg, null);
            // Avance jusqu'à la fin pour que les emplacements soient trouvés
            while (e.MoveNext()) { }
        }

        [Test]
        public void SetupRoom_PlacesWallsAccordingToFlags()
        {
            var rg = CreateRoomWithAnchors();

            RunPrivateInit(rg);

            var setup = rg.SetupRoomCoroutine(
                northDoor: true, southDoor: false, eastDoor: false, westDoor: false,
                isNorthExternal: false, isSouthExternal: true, isEastExternal: true, isWestExternal: true,
                placeNorthWall: true, placeSouthWall: false, placeEastWall: false, placeWestWall: false
            );
            while (setup.MoveNext()) { }

            Assert.IsTrue(rg.IsWallInstantiated(0), "Mur Nord devrait être instancié (porte)");
            Assert.IsFalse(rg.IsWallInstantiated(1));
            Assert.IsFalse(rg.IsWallInstantiated(2));
            Assert.IsFalse(rg.IsWallInstantiated(3));

            Object.DestroyImmediate(rg.gameObject);
        }
    }
}

