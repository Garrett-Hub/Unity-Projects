using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Vector3Extensions
{
    public static Vector3 With(this Vector3 source, float? x = null, float? y = null, float? z = null)
    {
        // Return new Vector3 with a swapped value
        return new Vector3(x ?? source.x, y ?? source.y, z ?? source.z);
    }

    public static Vector3 Flat(this Vector3 source)
    {
        // Return the Vector3 with y value set to 0
        return new Vector3(source.x, 0, source.z);
    }

    public static bool IsBehind(this Vector3 source, Vector3 forward)
    {
        // Takes a queried Vector3 and tests if it is behind forward
        return Vector3.Dot(source, forward) < 0f;
    }

    public static float SignedHorizontalAngleTo(this Vector3 source, Vector3 forward)
    {
        // Returns a signed angle between two Vector3 forwards ignoring y values
        return Vector2.SignedAngle(new Vector2(source.x, source.z), new Vector2(forward.x, forward.z));
    }
}
