using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace GameKit.UI
{
    public class CompassSystem : MonoBehaviour
    {
        [Header("Compass Settings")]
        [Tooltip("A manual reference to the compass Raw Image this script should be on.")]
        [SerializeField] RawImage compassImage;
        [Tooltip("How many pixels is 360 degrees in the compass image?  This should be half the image's width in pixels.")]
        [SerializeField] int fullCirclePixels;
        [Tooltip("A manual reference to the player (or player camera) that will be used to get a compass bearing.")]
        [SerializeField] Transform playerCameraTransform;
        [Tooltip("Recommended to spawn waypoints as children of the compass mask.")]
        [SerializeField] Transform waypointParentTransform;

        [Header("Compass Waypoint Pools")]
        [SerializeField] List<WaypointPool> waypointPools = new List<WaypointPool>();

        WaypointPoolHandler waypointPoolHandler = new WaypointPoolHandler();
        Dictionary<string, Queue<Waypoint>> poolDictionary;

        Transform playerTransform;
        float angleToPixelConversionRate;


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
            // Attempt to find compass image if no reference was manually set up
            if (!compassImage)
            {
                Debug.Log("No compass bar image reference set up.  Searching self for Raw Image component.");
                compassImage = GetComponent<RawImage>();
                if (!compassImage)
                {
                    Debug.LogError("No compass image found for compass bar.  Destroying this script.");
                    Destroy(this);
                }
            }

            // Camera.main is slow, so set this reference manually if possible
            if (!playerCameraTransform) playerCameraTransform = Camera.main.transform;

            // Get a reference to the player's transform for calculating angles
            playerTransform = Characters.PlayerManager.Instance.transform;

            // Set conversion rate for displaying waypoint markers on the compass
            angleToPixelConversionRate = fullCirclePixels / 360f;
        }

        void LateUpdate()
        {
            // Place wrapping compass image based on camera's rotation
            compassImage.uvRect = new Rect(playerCameraTransform.localEulerAngles.y / 720f, 0, 1, 1);

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

            // Permanently increase the size of the pool until there are enough waypoints for every target in list
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

        void UpdateWaypoint(Waypoint waypoint)
        {
            // Find angle between camera forward and player's Vector to waypoint target
            Vector3 targetDir = waypoint.target.transform.HorizontalDirectionTo(playerTransform);
            float angle = playerCameraTransform.SignedHorizontalAngleTo(targetDir);

            // Convert angle to pixels
            float angleToPixels = angle * angleToPixelConversionRate;

            // Set waypoint location
            waypoint.transform.localPosition = new Vector3(angleToPixels, 0, 0);
        }
    }
}