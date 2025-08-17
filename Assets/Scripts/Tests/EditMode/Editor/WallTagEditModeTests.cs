using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode.Editor
{
    public class WallTagEditModeTests
    {
        [Test]
        public void DefaultWallType_IsNormal()
        {
            var go = new GameObject("Wall");
            var wt = go.AddComponent<WallTag>();
            Assert.AreEqual(WallType.Normal, wt.wallType);
            Object.DestroyImmediate(go);
        }
    }
}

