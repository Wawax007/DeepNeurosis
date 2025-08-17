using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Tests.PlayMode
{
    public class FloorManagerPlayModeTests
    {
        private FloorManager CreateFloorManager(out BaseGenerator gen)
        {
            var fmGo = new GameObject("FloorManagerTest");
            var fm = fmGo.AddComponent<FloorManager>();
            var genGo = new GameObject("BaseGenerator");
            gen = genGo.AddComponent<BaseGenerator>();
            fm.baseGenerator = gen;
            return fm;
        }

        [Test]
        public void GoToStartRoom_SetsReady_AndActivatesStartRoom()
        {
            var fm = CreateFloorManager(out var gen);

            // Supprime une éventuelle sauvegarde persistante pour éviter LoadStartRoom et la recherche par tag
            string startJson = Path.Combine(Application.persistentDataPath, fm.saveFolderName, "startRoom.json");
            if (File.Exists(startJson)) File.Delete(startJson);

            // Crée l’objet StartRoom attendu par FloorManager
            var startRoom = new GameObject("StartRoom");
            // Un Rigidbody pour tester le toggling (non bloquant si pas présent)
            startRoom.AddComponent<Rigidbody>();
            startRoom.SetActive(false);

            // Fix: renseigne le champ privé startRoomObj pour permettre SetStartRoomActive(true)
            var fieldStartRoom = typeof(FloorManager).GetField("startRoomObj", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(fieldStartRoom, "Champ startRoomObj introuvable");
            fieldStartRoom.SetValue(fm, startRoom);

            Assert.IsFalse(gen.IsFloorReady);
            fm.GoToFloor(-2);

            Assert.IsTrue(gen.IsFloorReady, "BaseGenerator.IsFloorReady doit être true pour -2");
            Assert.IsTrue(startRoom.activeSelf, "StartRoom doit être activé");

            Object.DestroyImmediate(fm.gameObject);
            Object.DestroyImmediate(gen.gameObject);
            Object.DestroyImmediate(startRoom);
        }

        [Test]
        public void GoToExtractionPod_ActivatesExtractionObj_AndReady()
        {
            var fm = CreateFloorManager(out var gen);

            // Assure l’existence du dossier de sauvegarde utilisé par HasAnyConsoleBeenValidated()
            string dirPath = Path.Combine(Application.persistentDataPath, fm.saveFolderName);
            if (!Directory.Exists(dirPath)) Directory.CreateDirectory(dirPath);

            // Fournit StartRoom (utilisé par SaveAndUnloadCurrentFloor en préambule)
            var startRoom = new GameObject("StartRoom");

            // Assigne extractionPodObj via réflexion (champ privé sérialisé)
            var extraction = new GameObject("ExtractionPodRoot");
            var field = typeof(FloorManager).GetField("extractionPodObj", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(field, "Champ extractionPodObj introuvable");
            field.SetValue(fm, extraction);
            extraction.SetActive(false);

            fm.GoToFloor(2);

            Assert.IsTrue(gen.IsFloorReady, "BaseGenerator.IsFloorReady doit être true pour 2");
            Assert.IsTrue(extraction.activeSelf, "L’extractionPod doit être activé");

            Object.DestroyImmediate(fm.gameObject);
            Object.DestroyImmediate(gen.gameObject);
            Object.DestroyImmediate(extraction);
            Object.DestroyImmediate(startRoom);
        }
    }
}
