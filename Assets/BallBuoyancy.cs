using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallBuoyancy : MonoBehaviour
{

    public float buoyancyForce = 10f;
    public float ballDiameter = 0.2f;
    private Rigidbody rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Rigidbody component not found on " + gameObject.name);
        }
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Water") && rb != null && !rb.isKinematic)
        {
            float depth = Mathf.Clamp01((other.transform.position.y - transform.position.y) / ballDiameter);
            rb.AddForce(Vector3.down * buoyancyForce * depth, ForceMode.Acceleration);
        }
    }
}
