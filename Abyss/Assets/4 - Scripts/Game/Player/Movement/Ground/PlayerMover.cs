using UnityEngine;

namespace Game.Player.Movement.Ground
{
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

            public float StepHeight;           
            public float MinStepHeight;       
            public float StepDuration;        
            public float CheckDistance;       
            public float ColliderRadius;      
            public float ColliderYOffset;     
            public LayerMask GroundMask;      
            public float LowOriginOffset;      

        }

        private readonly Rigidbody _rb;
        private readonly MovementConfig _cfg;

        // step state
        private bool _isStepping;
        private float _stepElapsed;
        private float _stepDuration;
        private float _targetY;

        public PlayerMover(Rigidbody rb, MovementConfig cfg)
        {
            _rb = rb;
            _cfg = cfg;
            _isStepping = false;
        }

        public void ApplyMovement(Vector3 wishDir, bool isGrounded, Vector3 groundNormal, Vector3 currVel, float dt)
        {
            if (_isStepping)
            {
                ContinueStep(dt);
            }

            var currHor = new Vector3(currVel.x, 0f, currVel.z);
            var maxDeltaGround = _cfg.MaxAccelGround * dt;
            var maxDeltaAir = _cfg.MaxAccelAir * dt;

            if (isGrounded)
            {
                var n = groundNormal.normalized;
                var slopeAngle = Vector3.Angle(n, Vector3.up);

                if (slopeAngle <= _cfg.MaxWalkableAngle)
                {
                    if (!_isStepping)
                        TryStartStepUp(wishDir, dt);

                    var dirOnPlane = Vector3.ProjectOnPlane(wishDir, n);
                    if (dirOnPlane.sqrMagnitude > 0.0001f) dirOnPlane.Normalize();
                    else dirOnPlane = Vector3.zero;

                    var targetHor = new Vector3(dirOnPlane.x, 0f, dirOnPlane.z) * _cfg.MoveSpeed;
                    var delta = targetHor - currHor;
                    if (delta.magnitude > maxDeltaGround)
                        delta = delta.normalized * maxDeltaGround;

                    _rb.linearVelocity = new Vector3(currHor.x + delta.x, _rb.linearVelocity.y, currHor.z + delta.z);
                }
                else
                {
                    var slideDir = Vector3.ProjectOnPlane(Vector3.down, n).normalized;
                    var targetHor = slideDir * _cfg.SlideSpeed;
                    var delta = targetHor - currHor;
                    if (delta.magnitude > maxDeltaGround)
                        delta = delta.normalized * maxDeltaGround;
                    _rb.linearVelocity = new Vector3(currHor.x + delta.x, _rb.linearVelocity.y, currHor.z + delta.z);
                }
            }
            else
            {
                var targetHor = (wishDir.sqrMagnitude > 0.0001f ? wishDir.normalized : Vector3.zero) * _cfg.MoveSpeed * _cfg.AirControlMul;
                var delta = targetHor - currHor;
                if (delta.magnitude > maxDeltaAir) delta = delta.normalized * maxDeltaAir;
                _rb.linearVelocity = new Vector3(currHor.x + delta.x, _rb.linearVelocity.y, currHor.z + delta.z);
            }
        }

        private void TryStartStepUp(Vector3 wishDir, float dt)
        {
            Vector3 wishHor = new Vector3(wishDir.x, 0f, wishDir.z);
            if (wishHor.sqrMagnitude < 0.0005f) return;
            Vector3 moveDir = wishHor.normalized;

            Vector3 sphereCenter = _rb.position + Vector3.up * _cfg.ColliderYOffset;
            Vector3 lowOrigin = sphereCenter + Vector3.up * _cfg.LowOriginOffset;
            Vector3 highOrigin = lowOrigin + Vector3.up * _cfg.StepHeight;
            float checkDist = _cfg.CheckDistance;

            if (!Physics.Raycast(lowOrigin, moveDir, out RaycastHit hitLow, checkDist, _cfg.GroundMask, QueryTriggerInteraction.Ignore))
                return;
            if (hitLow.distance < 0.04f) return;

            if (Physics.Raycast(highOrigin, moveDir, out RaycastHit hitHigh, checkDist, _cfg.GroundMask, QueryTriggerInteraction.Ignore))
                return;

            float maxTopY = float.NegativeInfinity;
            bool anyFound = false;

            float[] offsets = new float[]
            {
        0.02f,
        Mathf.Clamp(_cfg.ColliderRadius * 0.5f, 0.02f, _cfg.ColliderRadius),
        _cfg.ColliderRadius + 0.02f
            };

            foreach (var offs in offsets)
            {
                Vector3 probeStart = hitLow.point + moveDir * offs + Vector3.up * (_cfg.StepHeight + 0.2f);
                if (Physics.Raycast(probeStart, Vector3.down, out RaycastHit hitDown, _cfg.StepHeight + 0.6f, _cfg.GroundMask, QueryTriggerInteraction.Ignore))
                {
                    anyFound = true;
                    if (hitDown.point.y > maxTopY) maxTopY = hitDown.point.y;
                }
            }

            if (!anyFound) return;

            float topY = maxTopY;

            float sphereBottomY = sphereCenter.y - _cfg.ColliderRadius;
            float desiredDelta = topY - sphereBottomY;
            if (desiredDelta < _cfg.MinStepHeight || desiredDelta > _cfg.StepHeight + 0.01f) return;

            Vector3 targetSphereCenter = sphereCenter + Vector3.up * desiredDelta;
            Collider[] hits = Physics.OverlapSphere(targetSphereCenter, Mathf.Max(0.01f, _cfg.ColliderRadius * 0.9f), _cfg.GroundMask, QueryTriggerInteraction.Ignore);
            if (hits.Length > 0)
            {
                bool obstructed = false;
                foreach (var c in hits)
                {
                    if (c == null) continue;
                    if (IsPartOfThis(c.transform)) continue;
                    obstructed = true; break;
                }
                if (obstructed) return;
            }

            float targetRbY = _rb.position.y + desiredDelta;
            StartStep(targetRbY, _cfg.StepDuration);

        }


        private void StartStep(float targetRbY, float duration)
        {
            _isStepping = true;
            _stepElapsed = 0f;
            _stepDuration = Mathf.Max(0.0001f, duration);
            _targetY = targetRbY;

            Vector3 v = _rb.linearVelocity;
            if (v.y < 0f) v.y = 0f;
            _rb.linearVelocity = v;
        }

        private void ContinueStep(float dt)
        {
            _stepElapsed += dt;
            float t = Mathf.Clamp01(_stepElapsed / _stepDuration);
            Vector3 cur = _rb.position;
            float newY = Mathf.Lerp(cur.y, _targetY, t); 
            Vector3 newPos = new Vector3(cur.x, newY, cur.z);
            _rb.MovePosition(newPos);

            if (t >= 1f - 1e-4f)
            {
                _isStepping = false;
                Vector3 v = _rb.linearVelocity;
                if (v.y < 0f) v.y = 0f;
                _rb.linearVelocity = v;
            }
        }

        private bool IsPartOfThis(Transform t)
        {
            while (t != null)
            {
                if (t == _rb.transform) return true;
                t = t.parent;
            }
            return false;
        }
    }
}
