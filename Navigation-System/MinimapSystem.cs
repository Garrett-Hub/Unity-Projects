using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameKit.UI
{
    public class MinimapSystem : MonoBehaviour
    {
        [SerializeField] Camera minimapCamera;
        [SerializeField] RadarSystem syncWithRadarSystem;
        [SerializeField] RectTransform playerPointerTransform;

        public bool upIsNorth;

        Transform mainCameraTransform;
        Transform playerTransform;

        void Start()
        {
            if (!minimapCamera) return;

            if (syncWithRadarSystem)
            {
                minimapCamera.orthographicSize = syncWithRadarSystem.RadiusInMeters;
                upIsNorth = syncWithRadarSystem.UpIsNorth;
            }

            mainCameraTransform = Camera.main.transform; // TODO: slow, set it up to be able to set camera manually
            playerTransform = Characters.PlayerManager.Instance.transform;
        }

        void LateUpdate()
        {
            float angle = 0;

            if (!upIsNorth)
            {
                // Find the angle between camera's forward and "North" for rotating the camera or player pointer
                angle = mainCameraTransform.SignedHorizontalAngleTo(Vector3.forward);
            }

            // Rotate minimap
            minimapCamera.transform.rotation = Quaternion.Euler(new Vector3(90f, -angle, 0f));

            // Find out which direction the player is facing
            angle = playerTransform.SignedHorizontalAngleTo(Vector3.forward) - angle;

            // Point arrow in the direction the player is facing
            playerPointerTransform.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        }
    }
}