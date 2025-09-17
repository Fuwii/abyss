using UnityEngine;

/// <summary>
/// Вынесенная логика применения движения (ground/slide/air). Сохраняет linearVelocity usage.
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

    private readonly Rigidbody rb;
    private readonly MovementConfig cfg;

    public PlayerMover(Rigidbody rb, MovementConfig config)
    {
        this.rb = rb;
        this.cfg = config;
    }

    public void ApplyMovement(Vector3 wishDir, bool isGrounded, Vector3 groundNormal, Vector3 currVel, float dt)
    {
        Vector3 currHor = new Vector3(currVel.x, 0f, currVel.z);
        float maxDeltaGround = cfg.MaxAccelGround * dt;
        float maxDeltaAir = cfg.MaxAccelAir * dt;

        if (isGrounded)
        {
            Vector3 n = groundNormal.normalized;
            float slopeAngle = Vector3.Angle(n, Vector3.up);

            if (slopeAngle <= cfg.MaxWalkableAngle)
            {
                Vector3 dirOnPlane = Vector3.ProjectOnPlane(wishDir, n);
                if (dirOnPlane.sqrMagnitude > 0.0001f) dirOnPlane.Normalize();
                else dirOnPlane = Vector3.zero;

                Vector3 targetHor = new Vector3(dirOnPlane.x, 0f, dirOnPlane.z) * cfg.MoveSpeed;
                Vector3 delta = targetHor - currHor;
                float deltaMag = delta.magnitude;
                if (deltaMag > maxDeltaGround)
                    delta = delta.normalized * maxDeltaGround;

                rb.linearVelocity = new Vector3(currHor.x + delta.x, currVel.y, currHor.z + delta.z);
            }
            else
            {
                Vector3 slideDir = Vector3.ProjectOnPlane(Vector3.down, n);
                if (slideDir.sqrMagnitude > 0.0001f) slideDir.Normalize();
                else slideDir = Vector3.zero;

                Vector3 targetHor = slideDir * cfg.SlideSpeed;
                Vector3 delta = targetHor - currHor;
                float deltaMag = delta.magnitude;
                float maxDeltaSlide = maxDeltaGround;
                if (deltaMag > maxDeltaSlide)
                    delta = delta.normalized * maxDeltaSlide;

                rb.linearVelocity = new Vector3(currHor.x + delta.x, currVel.y, currHor.z + delta.z);
            }
        }
        else
        {
            Vector3 targetHor = (wishDir.sqrMagnitude > 0.0001f ? wishDir.normalized : Vector3.zero) * cfg.MoveSpeed * cfg.AirControlMul;
            Vector3 delta = targetHor - currHor;
            float maxDelta = maxDeltaAir;
            if (delta.magnitude > maxDelta) delta = delta.normalized * maxDelta;

            rb.linearVelocity = new Vector3(currHor.x + delta.x, currVel.y, currHor.z + delta.z);
        }
    }
}
