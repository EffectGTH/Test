using UnityEngine;

public class Rotate : MonoBehaviour
{
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private int cubeCount = 10;
    [SerializeField] private bool isArranged = true;
    [SerializeField] private float distance = 30f;
    [SerializeField] private float radius = 5f;
    [SerializeField] private float speed = 50f;
    [SerializeField] private bool isClockwise = true;

    private Transform[] cubes;
    private float currentAngle = 0f;

    private void Awake()
    {
        if (cubePrefab == null)
        {
            Debug.LogError("Назначить префаб");
            return;
        }

        cubes = new Transform[cubeCount];

        for (int i = 0; i < cubeCount; i++)
        {
            GameObject cube = Instantiate(cubePrefab, transform);
            cubes[i] = cube.transform;
        }
    }

    private void Update()
    {
        if (cubes == null || cubes.Length == 0) return;

        int direction = isClockwise ? 1 : -1;
        currentAngle += direction * speed * Time.deltaTime;
        float angleDifference = isArranged ? 360f / cubes.Length : distance;

        for (int i = 0; i < cubes.Length; i++)
        {
            float angle = (currentAngle + (i * angleDifference)) * Mathf.Deg2Rad;

            Vector3 cubeOffset = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);

            cubes[i].position = transform.position + cubeOffset;
            cubes[i].LookAt(transform.position);
        }
    }
}
