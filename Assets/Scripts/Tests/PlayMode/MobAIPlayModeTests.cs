using NUnit.Framework;
using UnityEngine;
using PlayerScripts;
using System.Reflection;

namespace Tests.PlayMode
{
    public class MobAIPlayModeTests
    {
        private (MobAI mob, GameObject mobGo, GameObject playerGo, FirstPersonController fpc) CreateMobAndPlayer()
        {
            // Player setup
            var playerGo = new GameObject("Player");
            playerGo.tag = "Player";
            playerGo.transform.position = new Vector3(3f, 0f, 0f);
            playerGo.AddComponent<CharacterController>();
            var col = playerGo.AddComponent<CapsuleCollider>(); col.height = 2f; col.radius = 0.4f;
            var fpc = playerGo.AddComponent<FirstPersonController>();
            var cam = new GameObject("Camera").transform; cam.SetParent(playerGo.transform, false); fpc.cameraTransform = cam;
            playerGo.AddComponent<AudioSource>();

            // Mob setup (pas de NavMeshAgent nécessaire pour ces tests)
            var mobGo = new GameObject("Mob");
            mobGo.transform.position = Vector3.zero;
            var mob   = mobGo.AddComponent<MobAI>();

            // Minimal FOV/portée
            mob.player       = playerGo.transform;
            mob.viewDistance = 50f;
            mob.viewAngle    = 140f;
            mob.obstacleMask = 0; // Raycast sur tout

            return (mob, mobGo, playerGo, fpc);
        }

        private static bool InvokeDetect(MobAI mob, string methodName)
        {
            var mi = typeof(MobAI).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, $"Méthode privée {methodName} introuvable");
            return (bool)mi.Invoke(mob, null);
        }

        [Test]
        public void DetectsPlayerInFront_ReturnsTrue()
        {
            var (mob, mobGo, playerGo, fpc) = CreateMobAndPlayer();

            // Regarder le joueur
            mobGo.transform.forward = (playerGo.transform.position - mobGo.transform.position).normalized;

            // Vérifie la détection (vision)
            Assert.IsTrue(InvokeDetect(mob, "CanDetectPlayer"));

            Object.DestroyImmediate(mobGo);
            Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void HiddenPlayer_IsIgnored_ReturnsFalse()
        {
            var (mob, mobGo, playerGo, fpc) = CreateMobAndPlayer();

            // Cache le joueur
            fpc.isHidden = true;
            mobGo.transform.forward = (playerGo.transform.position - mobGo.transform.position).normalized;

            Assert.IsFalse(InvokeDetect(mob, "CanDetectPlayer"));

            Object.DestroyImmediate(mobGo);
            Object.DestroyImmediate(playerGo);
        }

        [Test]
        public void RecentlyHeardPlayer_ReturnsTrue()
        {
            var (mob, mobGo, playerGo, fpc) = CreateMobAndPlayer();

            // Oriente le mob hors cône de vision
            mobGo.transform.forward = - (playerGo.transform.position - mobGo.transform.position).normalized;

            // Simule l’audition récente
            var lastHeardField = typeof(MobAI).GetField("lastHeardTime", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(lastHeardField);
            lastHeardField.SetValue(mob, Time.time);

            Assert.IsTrue(InvokeDetect(mob, "CanDetectPlayer"));

            Object.DestroyImmediate(mobGo);
            Object.DestroyImmediate(playerGo);
        }
    }
}
