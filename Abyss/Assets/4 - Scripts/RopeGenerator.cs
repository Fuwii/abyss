using UnityEngine;

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
        Rigidbody prevBody = anchorRigidbody;

        for (int i = 0; i < segmentCount; i++)
        {
            Vector3 pos = transform.position + Vector3.down * (i * segmentSpacing);
            GameObject segment = Instantiate(segmentPrefab, pos, Quaternion.identity, transform);
            segment.name = "Segement"+i;
            Rigidbody rb = segment.GetComponent<Rigidbody>();
            HingeJoint joint = segment.GetComponent<HingeJoint>();

            joint.connectedBody = prevBody;

            prevBody = rb;
        }
    }
}