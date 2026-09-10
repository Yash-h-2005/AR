using UnityEngine;
using System;

namespace SurakshaAR.Training.Fire
{
    /// <summary>
    /// Component attached to 3D AR training prefabs.
    /// Handles selection raycasting, visual highlights, and interaction events.
    /// </summary>
    public class TrainingObject : MonoBehaviour
    {
        [SerializeField] private TrainingObjectType objectType;
        public TrainingObjectType ObjectType => objectType;

        [SerializeField] private string objectName = "AR Object";
        public string ObjectName => objectName;

        [SerializeField] private GameObject highlightEffect;

        public event Action<TrainingObject> OnObjectSelected;

        private Renderer objectRenderer;
        private Color originalColor;

        private void Awake()
        {
            objectRenderer = GetComponent<Renderer>();
            if (objectRenderer != null && objectRenderer.material != null)
            {
                originalColor = objectRenderer.material.color;
            }
        }

        public void SetHighlight(bool enable, Color color = default)
        {
            if (highlightEffect != null)
            {
                highlightEffect.SetActive(enable);
            }

            if (objectRenderer != null && objectRenderer.material != null)
            {
                if (enable)
                {
                    Color highlightColor = (color != default) ? color : Color.yellow;
                    objectRenderer.material.color = Color.Lerp(originalColor, highlightColor, 0.6f);
                }
                else
                {
                    objectRenderer.material.color = originalColor;
                }
            }
        }

        public void SelectObject()
        {
            Debug.Log($"[TrainingObject] Selected: {objectName} ({objectType})");
            OnObjectSelected?.Invoke(this);
        }

        private void OnMouseDown()
        {
            SelectObject();
        }
    }
}
