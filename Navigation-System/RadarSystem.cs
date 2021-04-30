using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameKit.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class RadarSystem : MonoBehaviour
    {
        enum RadarShape { Circle, Square }

        [Header("Radar Settings")]
        [Tooltip("Waypoint objects will be children of this transform.")]
        [SerializeField] Transform waypointParentTransform;
        [Tooltip("Radius of the circle or half the length of the sides of the square in meters.")]
        [SerializeField] float radiusDistanceInMeters = 30f;
        [Tooltip("If true, radar will NOT update based on camera's rotation.")]
        [SerializeField] bool upIsNorth = true;
        [Tooltip("Should the radar use square or circular boundaries")]
        [SerializeField] RadarShape shape = RadarShape.Circle; 

        [Header("Waypoint Pool Settings")]
        [Tooltip("Set up different types of waypoint pools to use here.")]
        [SerializeField] List<WaypointPool> waypointPools = new List<WaypointPool>();

        public float RadiusInMeters { get { return radiusDistanceInMeters; } }
        public bool UpIsNorth { get { return upIsNorth; } }

        WaypointPoolHandler waypointPoolHandler = new WaypointPoolHandler();
        Dictionary<string, Queue<Waypoint>> poolDictionary;

        Transform playerTransform;
        Transform cameraTransform;

        RectTransform rectTransform;
        float metersToPixelsConversionRate;

        private delegate void UpdateWaypoint(Waypoint waypoint);
        UpdateWaypoint WaypointUpdate;

        void Awake()
        {
            // Set up waypoint pools in Awake() so objects can find waypoints at Start() of scene
            poolDictionary = new Dictionary<string, Queue<Waypoint>>();

            // Create and add pool queues to the dictionary
            foreach (WaypointPool waypointPool in waypointPools)
                poolDictionary.Add(waypointPool.tag, waypointPoolHandler.CreateWaypointQueueFromPool(waypointPool, waypointParentTransform));
        }
        
        void Start()
        {
            if (!waypointParentTransform) waypointParentTransform = transform;

            cameraTransform = Camera.main.transform; // TODO get this another way
            playerTransform = Characters.PlayerManager.Instance.transform;

            rectTransform = GetComponent<RectTransform>();
            metersToPixelsConversionRate = (rectTransform.rect.width / 2) / radiusDistanceInMeters;

            // Set UpdateEvent to the correct method depending on what shape to use
            if (shape == RadarShape.Circle) WaypointUpdate = UpdateWaypointCircle;
            else WaypointUpdate = UpdateWaypointSquare;
        }

        void LateUpdate()
        {
            // Iterate through each waypoint pool in dictionary
            foreach (KeyValuePair<string, Queue<Waypoint>> entry in poolDictionary)
            {
                // Iterate through each active waypoint in pool and update its position
                foreach (Waypoint waypoint in entry.Value)
                {
                    if (waypoint.isActiveAndEnabled) WaypointUpdate(waypoint);
                }
            }
        }

        public void AddTargetToPool(string waypointPoolTag, GameObject target)
        {
            // Add the target to the correct waypool target list
            WaypointPool waypointPool = waypointPoolHandler.GetPoolByTag(waypointPoolTag, waypointPools);
            if (waypointPool == null) return;
            waypointPool.targets.Add(target);

            // Permanently increase the size of the pool until there are enough waypoints for every target
            while (waypointPool.targets.Count > waypointPool.size)
                waypointPoolHandler.AddWaypointToPoolQueue(waypointPool, poolDictionary[waypointPoolTag], waypointParentTransform);

            // Find an available waypoint to track the new target
            waypointPoolHandler.TrackNewTarget(target, poolDictionary[waypointPoolTag]);
        }

        public void RemoveTargetFromPool(string waypointPoolTag, GameObject target)
        {
            WaypointPool waypointPool = waypointPoolHandler.GetPoolByTag(waypointPoolTag, waypointPools);
            if (waypointPool == null) return;

            // Remove the target from the pool's target list and stop tracking it
            waypointPool.targets.Remove(target);
            waypointPoolHandler.StopTrackingTarget(target, poolDictionary[waypointPoolTag]);
        }

        void UpdateWaypointCircle(Waypoint waypoint)
        {
            // Find 2D distance to target
            float distance = waypoint.target.transform.HorizontalDistanceTo(playerTransform);
            float pixelDistance = distance * metersToPixelsConversionRate;

            // Find angle between camera forward (or "North") and player's Vector to waypoint target
            Vector3 targetDir = waypoint.target.transform.HorizontalDirectionTo(playerTransform);
            float angle = upIsNorth ? Vector3.forward.SignedHorizontalAngleTo(targetDir) : cameraTransform.SignedHorizontalAngleTo(targetDir);

            if (distance <= radiusDistanceInMeters)
            {
                // Disable arrow, show image
                waypoint.HideArrow();
                waypoint.waypointImage.enabled = true;
            }
            else
            {
                // If pointTowardsScreenEdge is true, keep waypoints enabled on edge of radar and point arrows away from center
                if (waypoint.pointTowardsScreenEdge)
                {
                    // Make pixel distance the radius of the radar and point arrow towards the edge
                    pixelDistance = radiusDistanceInMeters * metersToPixelsConversionRate;
                    waypoint.RotateArrow(Quaternion.Euler(0, 0, -angle));
                }
                else
                {
                    // Disable arrow and image when out of range
                    waypoint.HideArrow();
                    waypoint.waypointImage.enabled = false;
                    return;
                }
            }

            // Put waypoint at correct spot on radar
            float x = pixelDistance * Mathf.Cos((angle - 90) * Mathf.Deg2Rad);
            float y = pixelDistance * Mathf.Sin((angle + 90) * Mathf.Deg2Rad);
            waypoint.transform.localPosition = new Vector2(x, y);
        }

        void UpdateWaypointSquare(Waypoint waypoint)
        {
            // Turn target's position into a Vector2 point
            Vector2 targetPos = new Vector2(waypoint.target.transform.position.x, waypoint.target.transform.position.z);

            // Subtract the player's position so player will be at 0,0
            targetPos.x -= playerTransform.position.x;
            targetPos.y -= playerTransform.position.z;

            // Determine what direction "Up" on the radar will be in the real world, either "North" or camera forward
            Vector2 up = upIsNorth ? new Vector2(Vector3.forward.x, Vector3.forward.z) : 
                new Vector2(cameraTransform.forward.x, cameraTransform.forward.z);

            // Get the angle between "Up" and "North" and convert to radians
            float angle = Vector2.SignedAngle(up, new Vector2(Vector3.forward.x, Vector3.forward.z)) * Mathf.Deg2Rad;

            // Rotate targetPos based on angle
            float rotatedX = Mathf.Cos(angle) * targetPos.x - Mathf.Sin(angle) * targetPos.y;
            float rotatedY = Mathf.Sin(angle) * targetPos.x + Mathf.Cos(angle) * targetPos.y;
            targetPos.x = rotatedX;
            targetPos.y = rotatedY;

            // Check if targetPos is outside of the square
            if (targetPos.x > radiusDistanceInMeters || targetPos.y > radiusDistanceInMeters ||
                targetPos.x < -radiusDistanceInMeters || targetPos.y < -radiusDistanceInMeters)
            {
                // Don't show waypoint if pointTowardsScreenEdge is not enabled
                if (!waypoint.pointTowardsScreenEdge)
                {
                    waypoint.HideArrow();
                    waypoint.waypointImage.enabled = false;
                    return;
                }

                // Find slope of line towards target
                float m = targetPos.y / targetPos.x;

                // Solve for X to and Y for placing the waypoint
                float x = radiusDistanceInMeters / m;
                float y = radiusDistanceInMeters * m;

                // Right side
                if (targetPos.x > 0)
                {
                    // Upper right quadrant 1
                    if (targetPos.y > 0)
                    {
                        if (Mathf.Abs(targetPos.x) > Mathf.Abs(targetPos.y))
                        {
                            targetPos.x = radiusDistanceInMeters * metersToPixelsConversionRate;
                            targetPos.y = y * metersToPixelsConversionRate;
                        }
                        else
                        {
                            targetPos.x = x * metersToPixelsConversionRate;
                            targetPos.y = radiusDistanceInMeters * metersToPixelsConversionRate;
                        }
                    }
                    else // Lower right quadrant 4
                    {
                        if (Mathf.Abs(targetPos.x) > Mathf.Abs(targetPos.y))
                        {
                            targetPos.x = radiusDistanceInMeters * metersToPixelsConversionRate;
                            targetPos.y = y * metersToPixelsConversionRate;
                        }
                        else
                        {
                            targetPos.x = -x * metersToPixelsConversionRate;
                            targetPos.y = -radiusDistanceInMeters * metersToPixelsConversionRate;
                        }
                    }
                }
                else // Left side
                {
                    // Upper left quadrant 2
                    if (targetPos.y > 0)
                    {
                        if (Mathf.Abs(targetPos.x) > Mathf.Abs(targetPos.y))
                        {
                            targetPos.x = -radiusDistanceInMeters * metersToPixelsConversionRate;
                            targetPos.y = -y * metersToPixelsConversionRate;
                        }
                        else
                        {
                            targetPos.x = x * metersToPixelsConversionRate;
                            targetPos.y = radiusDistanceInMeters * metersToPixelsConversionRate;
                        }
                    }
                    else // Lower left quadrant 3
                    {
                        if (Mathf.Abs(targetPos.x) > Mathf.Abs(targetPos.y))
                        {
                            targetPos.x = -radiusDistanceInMeters * metersToPixelsConversionRate;
                            targetPos.y = -y * metersToPixelsConversionRate;
                        }
                        else
                        {
                            targetPos.x = -x * metersToPixelsConversionRate;
                            targetPos.y = -radiusDistanceInMeters * metersToPixelsConversionRate;
                        }
                    }
                }

                // Update waypoint position
                waypoint.transform.localPosition = new Vector2(targetPos.x, targetPos.y);

                // Find relative vector to target to get angle and point the arrow
                Vector3 relative;
                if (upIsNorth) relative = waypoint.target.transform.position - playerTransform.position;
                else relative = cameraTransform.InverseTransformPoint(waypoint.target.transform.position);

                angle = Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg;
                waypoint.RotateArrow(Quaternion.Euler(0, 0, -angle));
            }
            else // In bounds
            {
                waypoint.HideArrow();

                targetPos.x *= metersToPixelsConversionRate;
                targetPos.y *= metersToPixelsConversionRate;

                waypoint.transform.localPosition = new Vector2(targetPos.x, targetPos.y);
            }
        }
    }
}