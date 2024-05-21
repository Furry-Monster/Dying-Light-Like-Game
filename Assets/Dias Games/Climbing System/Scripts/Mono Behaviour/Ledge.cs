using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace DiasGames.Climbing
{
    public class Ledge : MonoBehaviour
    {
        [SerializeField] private List<Transform> grabPoints = new List<Transform>();
        [Tooltip("Set this parameter to true if you want to allow your character to jump without find another ledge.")]
        [SerializeField] private bool canFreelyJump = false;
        [SerializeField] private bool invisible = false;
        [SerializeField] private bool alwaysKeepRotation = false;
        [Header("Debug")]
        [SerializeField] private Color grabPointColor = Color.magenta;
        [SerializeField] private Color arrowColor = Color.magenta;
        [SerializeField] private float grabPointRadius = 0.1f;
        [SerializeField] private float arrowSize = 0.1f;

        private Quaternion _startRotation;

        public bool CanFreelyJump { get { return canFreelyJump; } }

        private void Awake()
        {
            if (invisible)
            {
                var meshes = GetComponentsInChildren<MeshRenderer>();
                foreach (var mesh in meshes)
                    mesh.enabled = false;
            }

            _startRotation = transform.rotation;
        }

        private void Update()
        {
            if (!alwaysKeepRotation) return;

            transform.rotation = _startRotation;
        }

        public Transform GetClosestPoint(Vector3 hitPoint, Vector3 normal)
        {
            if (grabPoints.Count == 0)
                return null;

            Transform closestGrab = grabPoints[0];
            float closestDistance = Vector3.Distance(closestGrab.position, hitPoint);
            foreach (var grab in grabPoints)
            {
                float distance = Vector3.Distance(grab.position, hitPoint);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestGrab = grab;
                }
            }

            if (closestGrab != null && !closestGrab.CompareTag("LedgeLimit")) 
            {
                if (normal != Vector3.zero &&
                    closestDistance > 0.25f &&
                    Vector3.Dot(closestGrab.forward, normal) > 0.7f)
                    return null;
            }

            return closestGrab;
        }

        public Transform GetClosestPoint(Vector3 hitPoint)
        {
            return GetClosestPoint(hitPoint, Vector3.zero);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (grabPoints.Count == 0) return;

            if (Vector3.Distance(Camera.current.transform.position, transform.position) > 15f)
                return;

            foreach (var grab in grabPoints)
            {
                Gizmos.color = grabPointColor;
                Gizmos.DrawWireSphere(grab.position, grabPointRadius);

                Handles.color = arrowColor;
                Handles.ArrowHandleCap(0, grab.position, Quaternion.LookRotation(grab.forward), arrowSize, EventType.Repaint);
            }

        }
#endif
    }
}