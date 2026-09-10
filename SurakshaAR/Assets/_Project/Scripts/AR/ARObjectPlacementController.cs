using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace SurakshaAR.AR
{
    /// <summary>
    /// Instantiates and repositions the single AR Test Object, attaching ARAnchor 
    /// components to prevent drift during testing.
    /// </summary>
    public class ARObjectPlacementController : MonoBehaviour
    {
        [Header("Prefab & Managers")]
        [SerializeField] private GameObject testObjectPrefab;
        [SerializeField] private ARAnchorManager anchorManager;
        [SerializeField] private ARRaycastPlacementController raycastController;

        public event Action<Vector3> OnObjectPlacedOrMoved;

        private GameObject placedObject;
        private ARAnchor currentAnchor;

        private void Awake()
        {
            if (anchorManager == null)
            {
                anchorManager = GetComponent<ARAnchorManager>();
            }
            if (raycastController == null)
            {
                raycastController = GetComponent<ARRaycastPlacementController>();
            }
        }

        private void OnEnable()
        {
            if (raycastController != null)
            {
                raycastController.OnValidPlacementTap += HandlePlacementTap;
            }
        }

        private void OnDisable()
        {
            if (raycastController != null)
            {
                raycastController.OnValidPlacementTap -= HandlePlacementTap;
            }
        }

        private void HandlePlacementTap(Pose pose, ARPlane plane)
        {
            if (placedObject == null)
            {
                if (testObjectPrefab != null)
                {
                    placedObject = Instantiate(testObjectPrefab, pose.position, pose.rotation);
                }
                else
                {
                    // Fallback procedural creation if prefab is not assigned
                    placedObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    placedObject.name = "AR_Test_Object";
                    placedObject.transform.position = pose.position;
                    placedObject.transform.rotation = pose.rotation;
                    placedObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f); // 20cm cube

                    var renderer = placedObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material.color = new Color(0.9f, 0.4f, 0.1f); // Industrial Safety Amber/Orange
                    }
                }
            }
            else
            {
                // Reposition existing test object
                placedObject.transform.position = pose.position;
                placedObject.transform.rotation = pose.rotation;
            }

            // Anchor management to ensure spatial stability
            if (anchorManager != null && plane != null)
            {
                if (currentAnchor != null)
                {
                    Destroy(currentAnchor);
                }
                currentAnchor = anchorManager.AttachAnchor(plane, pose);
                if (currentAnchor != null && placedObject != null)
                {
                    placedObject.transform.parent = currentAnchor.transform;
                }
            }

            OnObjectPlacedOrMoved?.Invoke(pose.position);
        }

        public bool IsObjectPlaced => placedObject != null;
    }
}
