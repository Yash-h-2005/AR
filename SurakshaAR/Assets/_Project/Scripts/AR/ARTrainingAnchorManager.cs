using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace SurakshaAR.AR
{
    /// <summary>
    /// Manages spatial ARAnchor lifecycle for training objects.
    /// Ensures zero spatial drift when workers physically walk around objects with the phone camera.
    /// </summary>
    public class ARTrainingAnchorManager : MonoBehaviour
    {
        public static ARTrainingAnchorManager Instance { get; private set; }

        [SerializeField] private ARAnchorManager anchorManager;
        private readonly Dictionary<GameObject, ARAnchor> objectAnchors = new Dictionary<GameObject, ARAnchor>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (anchorManager == null)
            {
                anchorManager = FindFirstObjectByType<ARAnchorManager>();
            }
        }

        public ARAnchor AnchorObjectToWorld(GameObject targetObject, Vector3 position, Quaternion rotation)
        {
            if (targetObject == null) return null;

            targetObject.transform.position = position;
            targetObject.transform.rotation = rotation;

            if (anchorManager != null)
            {
                // Try attaching ARAnchor
                ARAnchor newAnchor = targetObject.GetComponent<ARAnchor>();
                if (newAnchor == null)
                {
                    newAnchor = targetObject.AddComponent<ARAnchor>();
                }

                objectAnchors[targetObject] = newAnchor;
                Debug.Log($"[ARTrainingAnchorManager] Attached ARAnchor to: {targetObject.name} at {position}");
                return newAnchor;
            }

            return null;
        }

        public void RemoveAnchor(GameObject targetObject)
        {
            if (targetObject != null && objectAnchors.TryGetValue(targetObject, out ARAnchor anchor))
            {
                if (anchor != null)
                {
                    Destroy(anchor);
                }
                objectAnchors.Remove(targetObject);
            }
        }
    }
}
