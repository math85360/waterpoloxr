using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallBuoyancy : MonoBehaviour
{
    [System.Serializable]
    public struct ElementParameters
    {
        public float linearDamping;
        public float angularDamping;
    }
    public float buoyancyForce = 1f;
    public float ballDiameter = 0.2f;
    private Rigidbody rb;
    private bool inWater;
    public ElementParameters AirParameters;
    public ElementParameters WaterParameters;
    private Vector3 invertedGravity;
    public float forceLimiter = 5f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component not found on " + gameObject.name);
        }
    }
    void OnEnable()
    {
        invertedGravity = Physics.gravity * -1f;
    }

    bool IsWater(Collider other)
    {
        return other.CompareTag("Water");
    }

    void OnTriggerStay(Collider other)
    {
        // Debug.Log("OnTriggerStay: " + other.gameObject.name);
        if (IsWater(other) && rb != null && !rb.isKinematic)
        {
            float depth = ballDiameter + other.transform.position.y - transform.position.y;
            if (depth > 0)
            {
                float submergedRatio = Mathf.Clamp01(depth * forceLimiter);
                float forceMagnitude = submergedRatio * buoyancyForce;
                // Debug.Log($"forceMagnitude: {forceMagnitude} submergedRatio: {submergedRatio} depth: {depth} invertedGravity: {invertedGravity}");
                rb.AddForce(invertedGravity * forceMagnitude, ForceMode.Acceleration);
            }
        }
    }
    void SetParameters(ElementParameters args)
    {
        rb.linearDamping = args.linearDamping;
        rb.angularDamping = args.angularDamping;
    }

    void OnTriggerEnter(Collider other)
    {
        // Debug.Log("OnTriggerEnter: " + other.gameObject.name);
        if (IsWater(other))
        {
            SetParameters(WaterParameters);
        }
    }
    void OnTriggerExit(Collider other)
    {
        // Debug.Log("OnTriggerExit: " + other.gameObject.name);
        if (IsWater(other))
        {
            SetParameters(AirParameters);
        }
    }

}
