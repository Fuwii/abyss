using Game.Mechanics.Interactables.Tools;
using UnityEngine;

namespace Game.Player.Movement.Climbing
{
    public class ClimbableObject : MonoBehaviour, IInteractable
    {
        [Header("Climb Settings")]
        public ClimbType type = ClimbType.Rope;
        public Transform attachPoint; // ����� / ��� ������

        [Header("Grab")]
        [Tooltip("���������� �� ��� ������, �� ������� ����� '��������' (� ������).")]
        public float attachDistance = 1f;

        public void OnFocusEnter(GameObject player) { }
        public void OnFocusExit(GameObject player) { }

        public void Interact(GameObject player)
        {
            var climber = player.GetComponent<FpsPlayerClimbing>();
            if (climber != null)
            {
                climber.StartClimbOnObject(this);
            }
        }

        public Vector3 GetAttachPosition()
        {
            return attachPoint != null ? attachPoint.position : transform.position;
        }

        public Vector3 GetAttachPosition(Vector3 playerPosition)
        {
            if (attachPoint == null) return transform.position;

            var climbDir = GetClimbDirection().normalized;
            var axisOrigin = attachPoint.position;

            var t = Vector3.Dot(playerPosition - axisOrigin, climbDir);
            var nearestOnAxis = axisOrigin + climbDir * t;

            var radial = playerPosition - nearestOnAxis;
            if (radial.sqrMagnitude < 1e-6f)
            {
                radial = attachPoint.right;
            }

            radial = radial.normalized;

            return nearestOnAxis + radial * attachDistance;
        }

        public Vector3 GetClimbDirection()
        {
            return type == ClimbType.Chain ? Vector3.up : transform.up;
        }
    }
}