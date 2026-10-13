using UnityEngine;

public class Dice : MonoBehaviour
{
    private Rigidbody rb;
    private Vector3 startPosition;
    private Quaternion startRotation;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;
        startRotation = transform.rotation;
        
        rb.isKinematic = true; 
    }

    public void ResetDice()
    {
        rb.isKinematic = true; 
        rb.linearVelocity = Vector3.zero; 
        rb.angularVelocity = Vector3.zero; 
        
        transform.position = startPosition;
        transform.rotation = startRotation;
    }

    public void Roll(float minForce, float maxForce, float minTorque, float maxTorque)
    {
        rb.isKinematic = false; 

        var randomDirection = new Vector3(Random.Range(-1f, 1f), 1f, Random.Range(-1f, 1f)).normalized;
        var force = Random.Range(minForce, maxForce);
        
        var randomTorque = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        var torque = Random.Range(minTorque, maxTorque);

        rb.AddForce(randomDirection * force, ForceMode.Impulse);
        rb.AddTorque(randomTorque * torque, ForceMode.Impulse);
    }

    public bool IsSleeping()
    {
        return rb.IsSleeping();
    }

    public int GetFaceValue()
    {
        var directions = new Vector3[] { transform.up, -transform.up, transform.forward, -transform.forward, transform.right, -transform.right };
        var values = new int[] { 1, 6, 2, 5, 3, 4 };

        var maxDot = -1f;
        var bestValue = 1;

        for (var i = 0; i < directions.Length; i++)
        {
            var dot = Vector3.Dot(directions[i], Vector3.up);
            if (dot > maxDot)
            {
                maxDot = dot;
                bestValue = values[i];
            }
        }
        return bestValue;
    }
}