using UnityEngine;

namespace Game.Player.Movement.Ground
{
    /// <summary>
    /// ���������� ������ ���������� �������� (ground/slide/air). ��������� linearVelocity usage.
    /// </summary>
    public class PlayerMover
    {
        public struct MovementConfig
        {
            public float MoveSpeed;
            public float AirControlMul;
            public float MaxAccelGround;
            public float MaxAccelAir;
            public float MaxWalkableAngle;
            public float SlideSpeed;
        }

        private readonly Rigidbody _rb;
        private readonly MovementConfig _cfg;

        public PlayerMover(Rigidbody rb, MovementConfig config)
        {
            this._rb = rb;
            this._cfg = config;
        }

        public void ApplyMovement(Vector3 wishDir, bool isGrounded, Vector3 groundNormal, Vector3 currVel, float dt)
        {
            var currHor = new Vector3(currVel.x, 0f, currVel.z);
            var maxDeltaGround = _cfg.MaxAccelGround * dt;
            var maxDeltaAir = _cfg.MaxAccelAir * dt;

            if (isGrounded)
            {
                var n = groundNormal.normalized;
                var slopeAngle = Vector3.Angle(n, Vector3.up);

                if (slopeAngle <= _cfg.MaxWalkableAngle)
                {
                    var dirOnPlane = Vector3.ProjectOnPlane(wishDir, n);
                    if (dirOnPlane.sqrMagnitude > 0.0001f) dirOnPlane.Normalize();
                    else dirOnPlane = Vector3.zero;

                    var targetHor = new Vector3(dirOnPlane.x, 0f, dirOnPlane.z) * _cfg.MoveSpeed;
                    var delta = targetHor - currHor;
                    var deltaMag = delta.magnitude;
                    if (deltaMag > maxDeltaGround)
                        delta = delta.normalized * maxDeltaGround;

                    _rb.linearVelocity = new Vector3(currHor.x + delta.x, currVel.y, currHor.z + delta.z);
                }
                else
                {
                    var slideDir = Vector3.ProjectOnPlane(Vector3.down, n);
                    if (slideDir.sqrMagnitude > 0.0001f) slideDir.Normalize();
                    else slideDir = Vector3.zero;

                    var targetHor = slideDir * _cfg.SlideSpeed;
                    var delta = targetHor - currHor;
                    var deltaMag = delta.magnitude;
                    var maxDeltaSlide = maxDeltaGround;
                    if (deltaMag > maxDeltaSlide)
                        delta = delta.normalized * maxDeltaSlide;

                    _rb.linearVelocity = new Vector3(currHor.x + delta.x, currVel.y, currHor.z + delta.z);
                }
            }
            else
            {
                var targetHor = (wishDir.sqrMagnitude > 0.0001f ? wishDir.normalized : Vector3.zero) * _cfg.MoveSpeed * _cfg.AirControlMul;
                var delta = targetHor - currHor;
                var maxDelta = maxDeltaAir;
                if (delta.magnitude > maxDelta) delta = delta.normalized * maxDelta;

                _rb.linearVelocity = new Vector3(currHor.x + delta.x, currVel.y, currHor.z + delta.z);
            }
        }
    }
}