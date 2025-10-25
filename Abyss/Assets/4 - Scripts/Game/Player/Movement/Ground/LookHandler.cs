using Game.Player.Movement.Climbing;
using UnityEngine;

namespace Game.Player.Movement.Ground
{
    public class LookHandler : MonoBehaviour
    {
        private Transform _body;
        private Camera _cam;
        private float _sensitivity;
        private float _pitchMin, _pitchMax;

        public void Setup(Transform bodyTransform, Camera playerCamera, float mouseSens, float pMin, float pMax)
        {
            _body = bodyTransform;
            _cam = playerCamera;
            _sensitivity = mouseSens;
            _pitchMin = pMin;
            _pitchMax = pMax;
        }

        public void ApplyLook(Vector2 lookDelta, ref float yaw, ref float pitch, FpsPlayerClimbing climbingSystem)
        {
            yaw += lookDelta.x * _sensitivity;
            pitch += -lookDelta.y * _sensitivity;
            pitch = Mathf.Clamp(pitch, _pitchMin, _pitchMax);

            if (climbingSystem != null && climbingSystem.state == FpsPlayerClimbing.PlayerState.Climbing)
            {
                if (_cam) _cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }
            else
            {
                if (_cam) _cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }
    }
}