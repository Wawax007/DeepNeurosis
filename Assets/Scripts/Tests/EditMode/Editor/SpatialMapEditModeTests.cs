using NUnit.Framework;

namespace Tests.EditMode.Editor
{
    public class SpatialMapEditModeTests
    {
        [Test]
        public void Fits_TrueBeforeStamp_FalseAfterStamp()
        {
            var map = new SpatialMap(10, 10);
            bool[,] mask = new bool[3, 2];
            mask[0, 0] = true; mask[1, 0] = true; mask[2, 1] = true; // sparse L-shape

            int gx = 4, gz = 5;

            Assert.IsTrue(map.Fits(mask, gx, gz));
            map.Stamp(mask, gx, gz);
            Assert.IsFalse(map.Fits(mask, gx, gz));
        }

        [Test]
        public void MarkingCells_AffectsIsFree_AndFits()
        {
            var map = new SpatialMap(6, 6);
            Assert.IsTrue(map.IsFree(2, 3));
            map.Mark(2, 3);
            Assert.IsFalse(map.IsFree(2, 3));

            bool[,] mask = new bool[2, 2];
            mask[0, 0] = true; mask[1, 1] = true;

            // Place so that one occupied cell overlaps
            Assert.IsFalse(map.Fits(mask, 1, 2));
            // But shifting avoids the occupied one
            Assert.IsTrue(map.Fits(mask, 3, 3));
        }

        [Test]
        public void Stamp_SetsAllMaskedCellsOccupied()
        {
            var map = new SpatialMap(8, 8);
            bool[,] mask = new bool[3, 3];
            for (int x = 0; x < 3; x++)
                for (int z = 0; z < 3; z++)
                    mask[x, z] = (x + z) % 2 == 0; // checker pattern

            int gx = 1, gz = 2;
            map.Stamp(mask, gx, gz);

            for (int x = 0; x < 3; x++)
            for (int z = 0; z < 3; z++)
            {
                if (mask[x, z])
                    Assert.IsFalse(map.IsFree(gx + x, gz + z));
                else
                    Assert.IsTrue(map.IsFree(gx + x, gz + z));
            }
        }
    }
}

