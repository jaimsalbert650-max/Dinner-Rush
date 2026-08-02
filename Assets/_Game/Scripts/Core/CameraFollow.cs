using UnityEngine;

namespace DinnerRush
{
    /// <summary>Smoothly keeps the camera centered on a target (the player).</summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.15f;

        private Vector3 _velocity;
        private Vector3 _shake;

        public void SetTarget(Transform t) => target = t;

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 basePos = transform.position - _shake;   // undo last frame's shake
            Vector3 goal = new Vector3(target.position.x, target.position.y, basePos.z);
            basePos = Vector3.SmoothDamp(basePos, goal, ref _velocity, smoothTime);
            _shake = CameraShake.Instance != null ? CameraShake.Instance.Offset : Vector3.zero;
            transform.position = basePos + _shake;
        }
    }
}
