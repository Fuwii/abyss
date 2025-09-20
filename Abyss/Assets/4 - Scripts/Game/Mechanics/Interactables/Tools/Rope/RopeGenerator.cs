using UnityEngine;

namespace Game.Mechanics.Interactables.Tools.Rope
{
    public class RopeGenerator : MonoBehaviour
    {
        [Header("Prefabs & Settings")]
        public GameObject segmentPrefab;
        public int segmentCount = 10;
        public float segmentSpacing = 0.5f;

        [Header("Anchor")]
        public Rigidbody anchorRigidbody;

        void Start()
        {
            GenerateRope();
        }

        void GenerateRope()
        {
            var prevBody = anchorRigidbody;

            for (var i = 0; i < segmentCount; i++)
            {
                var pos = transform.position + Vector3.down * (i * segmentSpacing);
                var segment = Instantiate(segmentPrefab, pos, Quaternion.identity, transform);

                segment.name = "Segment" + i;

                var rb = segment.GetComponent<Rigidbody>();
                var joint = segment.GetComponent<HingeJoint>();

                joint.connectedBody = prevBody;

                prevBody = rb;
            }
        }
    }
}