using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TransformExtensions
{
    public static Vector3 DirectionTo(this Transform source, Vector3 destination)
    {
        // Returns a normalized direction between a transform and a Vector3
        return Vector3.Normalize(destination - source.position);
    }

    public static Vector3 HorizontalDirectionTo(this Transform source, Transform transform)
    {
        // Find Vector3 direction between to transforms, ignoring Y coordinates
        return new Vector3(source.position.x, transform.position.y, source.position.z)
                - transform.position;
    }

    public static float HorizontalDistanceTo(this Transform source, Transform transform)
    {
        // Returns a distance between two transforms, ignoring Y positions
        return Vector3.Distance(source.position,
                new Vector3(transform.position.x, source.position.y, transform.position.z));
    }

    public static float SignedHorizontalAngleTo(this Transform source, Transform transform)
    {
        // Return a signed angle between two transform forwards, ignoring Y positions
        return Vector3.SignedAngle(source.forward.Flat(), transform.forward.Flat(), Vector3.up);
    }

    public static float SignedHorizontalAngleTo(this Transform source, Vector3 forward)
    {
        // Return a signed angle between a transform's forward and another Vector3 forward
        return Vector3.SignedAngle(source.forward.Flat(), forward.Flat(), Vector3.up);
    }
}
