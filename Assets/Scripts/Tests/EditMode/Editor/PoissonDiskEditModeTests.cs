using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode.Editor
{
    public class PoissonDiskEditModeTests
    {
        [Test]
        public void Generate_PointsWithinArea_AndRespectMinDistance()
        {
            var area = new Vector2Int(16, 12);
            float minDist = 1.5f;
            var rng = new System.Random(12345);

            var pts = PoissonDisk.Generate(area, minDist, rng);

            Assert.That(pts, Is.Not.Null);
            Assert.That(pts.Count, Is.GreaterThan(0));

            foreach (var p in pts)
            {
                Assert.GreaterOrEqual(p.x, 0f);
                Assert.GreaterOrEqual(p.y, 0f);
                Assert.Less(p.x, area.x + 1e-4f);
                Assert.Less(p.y, area.y + 1e-4f);
            }

            for (int i = 0; i < pts.Count; i++)
            for (int j = i + 1; j < pts.Count; j++)
                Assert.GreaterOrEqual((pts[i] - pts[j]).magnitude, minDist - 1e-4f);
        }

        [Test]
        public void Generate_IsDeterministic_WithFixedSeed()
        {
            var area = new Vector2Int(12, 10);
            float minDist = 1.2f;
            var rng1 = new System.Random(42);
            var rng2 = new System.Random(42);

            var a = PoissonDisk.Generate(area, minDist, rng1);
            var b = PoissonDisk.Generate(area, minDist, rng2);

            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
                Assert.Less((a[i] - b[i]).sqrMagnitude, 1e-10f);
        }

        [Test]
        public void Generate_HighMinDist_YieldsZeroOrOnePoint()
        {
            var area = new Vector2Int(3, 3);
            float minDist = 10f;
            var rng = new System.Random(7);

            var pts = PoissonDisk.Generate(area, minDist, rng);

            Assert.LessOrEqual(pts.Count, 1);
        }

        [Test]
        public void Generate_ZeroMinDist_FillsTargetDensity()
        {
            var area = new Vector2Int(20, 10);
            float minDist = 0f;
            var rng = new System.Random(123);
            int expected = Mathf.CeilToInt(area.x * area.y * 0.35f);

            var pts = PoissonDisk.Generate(area, minDist, rng);

            Assert.AreEqual(expected, pts.Count);
        }

        [Test]
        public void Generate_ZeroArea_ReturnsEmpty()
        {
            var area = new Vector2Int(0, 10);
            float minDist = 1f;
            var rng = new System.Random(1);

            var pts = PoissonDisk.Generate(area, minDist, rng);
            Assert.IsNotNull(pts);
            Assert.AreEqual(0, pts.Count);

            area = new Vector2Int(10, 0);
            pts = PoissonDisk.Generate(area, minDist, rng);
            Assert.IsNotNull(pts);
            Assert.AreEqual(0, pts.Count);
        }
    }
}
