using UnityEngine;

namespace DiasGames.Components
{
    public class CameraTargetFollow : MonoBehaviour
    {
        public enum UpdateMode { FixedUpdate, LateUpdate}

        [SerializeField] private UpdateMode updateMode = UpdateMode.FixedUpdate;

        private Transform _follow;
        private Vector3 _offset;

        private void Awake()
        {
            _follow = transform.parent;
            _offset = transform.localPosition;

            transform.parent = null;
        }

        private void FixedUpdate()
        {
            if (updateMode == UpdateMode.FixedUpdate)
                transform.position = _follow.position + _offset;
        }

        private void LateUpdate()
        {
            if (updateMode == UpdateMode.LateUpdate)
                transform.position = _follow.position + _offset;
        }
    }
}