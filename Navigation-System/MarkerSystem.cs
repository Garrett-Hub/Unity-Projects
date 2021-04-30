using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameKit.Characters;

namespace GameKit.UI
{
    public class MarkerSystem : MonoBehaviour
    {
        [Header("Waypoint Display Settings")]
        [Tooltip("Automatically set to main camera if left blank.")]
        [SerializeField] Camera cam;
        [Tooltip("Sets the screen bounds width for displaying waypoints.  Smaller numbers use less screen area.")]
        [SerializeField] [Range(0.1f, 1f)] float screenUsageX = 0.8f;
        [Tooltip("Sets the screen bounds height for displaying waypoints.  Smaller numbers use less screen area.")]
        [SerializeField] [Range(0.1f, 1f)] float screenUsageY = 0.7f;

        [Header("Waypoint Pool Settings")]
        [Tooltip("Set up different types of waypoint pools to use here.")]
        [SerializeField] List<WaypointPool> waypointPools = new List<WaypointPool>();

        WaypointPoolHandler waypointPoolHandler = new WaypointPoolHandler();
        Dictionary<string, Queue<Waypoint>> poolDictionary;

        float deadZoneX, deadZoneY;

        void Awake()
        {
            // Set up waypoint pools in Awake() so objects can find waypoints at Start() of scene
            poolDictionary = new Dictionary<string, Queue<Waypoint>>();

            // Create and add pool queues to the dictionary
            foreach (WaypointPool waypointPool in waypointPools)
                poolDictionary.Add(waypointPool.tag, waypointPoolHandler.CreateWaypointQueueFromPool(waypointPool, transform));
        }

        void Start()
        {
            // Camera reference: 'Camera.main' is slow so set this reference up manually if possible
            if (!cam) cam = Camera.main;

            // Find width of borders outside the screen area that will display the waypoints
            deadZoneX = (Screen.width - (Screen.width * screenUsageX)) / 2;
            deadZoneY = (Screen.height - (Screen.height * screenUsageY)) / 2;
        }

        void LateUpdate()
        {
            // Iterate through each waypoint pool in dictionary
            foreach (KeyValuePair<string, Queue<Waypoint>> entry in poolDictionary)
            {
                // Iterate through each active waypoint in pool and update its position
                foreach (Waypoint waypoint in entry.Value)
                {
                    if (waypoint.isActiveAndEnabled) UpdateWaypoint(waypoint);
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
                waypointPoolHandler.AddWaypointToPoolQueue(waypointPool, poolDictionary[waypointPoolTag], transform);

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

        void UpdateWaypoint(Waypoint waypoint)
        {
            // Get screen position of the waypoint's target position
            Vector3 screenPos = waypoint.target.transform.position + waypoint.offset;
            screenPos = cam.WorldToScreenPoint(new Vector3(screenPos.x, screenPos.y, screenPos.z));

            // If it is on screen and within bounds, put waypoint indicator where target is on the screen
            if (screenPos.z > 0 && screenPos.x > deadZoneX && screenPos.x < (Screen.width - deadZoneX) &&
                                   screenPos.y > deadZoneY && screenPos.y < (Screen.height - deadZoneY))
            {
                waypoint.transform.position = screenPos;

                // Show distance to target
                if (waypoint.showDistanceInBounds) UpdateWaypointDistance(true, waypoint);
                else UpdateWaypointDistance(false, waypoint);

                // Hide arrow when target is within bounds
                waypoint.HideArrow();
            }
            // Else, off-screen or out of bounds
            else
            {
                if (screenPos.z < 0) screenPos *= -1; // Flip everything if object is behind camera

                Vector3 screenCenter = new Vector3(Screen.width, Screen.height, 0) / 2;

                // Make (0,0) the center of the screen instead of bottom left corner for calculating
                screenPos -= screenCenter;

                // Find angle from center to target
                float angle = Mathf.Atan2(screenPos.y, screenPos.x);
                angle -= 90 * Mathf.Deg2Rad;

                float cos = Mathf.Cos(angle);
                float sin = -Mathf.Sin(angle);
                float m = cos / sin;

                Vector3 screenBounds = new Vector3(screenCenter.x * screenUsageX, screenCenter.y * screenUsageY, 0);

                // Check up and down first
                if (cos > 0)
                {   // Up
                    screenPos = new Vector3(screenBounds.y / m, screenBounds.y, 0);
                }
                else
                {   // Down
                    screenPos = new Vector3(-screenBounds.y / m, -screenBounds.y, 0);
                }
                // If out of bounds, point to appropriate side
                if (screenPos.x > screenBounds.x)
                {   // Right
                    screenPos = new Vector3(screenBounds.x, screenBounds.x * m, 0);
                }
                else if (screenPos.x < -screenBounds.x)
                {   // Left
                    screenPos = new Vector3(-screenBounds.x, -screenBounds.x * m, 0);
                }

                // Readjust screen coordinates so 0,0 is in the bottom left corner
                screenPos += screenCenter;

                // Move waypoint image to correct screen position
                waypoint.transform.position = screenPos;

                // Rotate arrow towards target
                if (waypoint.pointTowardsScreenEdge) waypoint.RotateArrow(Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg));
                else waypoint.HideArrow();

                // Show distance to target
                if (waypoint.showDistanceOutOfBounds) UpdateWaypointDistance(true, waypoint);
                else UpdateWaypointDistance(false, waypoint);
            }
        }

        void UpdateWaypointDistance(bool update, Waypoint waypoint)
        {
            // If update, find distance between player's transform and waypoint's target position
            if (update)
                waypoint.UpdateDistance(Vector3.Distance(
                       PlayerManager.Instance.transform.position, // Player's transform
                       waypoint.target.transform.position)); // Waypoint target position
            else // Don't show distance
                waypoint.UpdateDistance(0, false);
        }
    }
}