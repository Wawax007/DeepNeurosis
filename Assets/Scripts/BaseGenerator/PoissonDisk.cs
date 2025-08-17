using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class PoissonDisk
{
    public static List<Vector2> Generate(Vector2Int area, float minDist, System.Random rng, int maxAttempts = 30)
    {
        var pts = new List<Vector2>();
        int target = Mathf.CeilToInt(area.x * area.y * 0.35f); // densité arbitraire

        int attempts = 0;
        while (pts.Count < target && attempts < target * maxAttempts)
        {
            attempts++;
            Vector2 p = new((float)rng.NextDouble() * area.x, (float)rng.NextDouble() * area.y);
            bool ok = true;
            foreach (var q in pts) if ((p - q).sqrMagnitude < minDist * minDist) { ok = false; break; }
            if (ok) pts.Add(p);
        }
        return pts;
    }
}