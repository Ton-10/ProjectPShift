using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShiftCollideFXScript : MonoBehaviour
{
    public GameObject prefab; // The prefab to clone
   
    public int cloneCount = 10; // Number of clones
    public float radius = 5f; // Radius of the circle
    public Vector3 axis = Vector3.up; // Axis to rotate around
    public float animationInterval = 2f; // Interval to change the animation trigger
    public float individualRotation = 0f; // Rotation to apply to each individual clone
    public GameObject SpeedUpEffect;

    private List<GameObject> clones = new List<GameObject>();
    private GameObject fxObject;
    private Transform target;  // The target object to position around

    void Start()
    {
    }

    void CreateClones()
    {
        for (int i = 0; i < cloneCount; i++)
        {
            float angle = i * Mathf.PI * 2 / cloneCount;
            Vector3 direction;

            if (axis == Vector3.up)
            {
                direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            }
            else if (axis == Vector3.right)
            {
                direction = new Vector3(0, Mathf.Cos(angle), Mathf.Sin(angle));
            }
            else // assuming axis == Vector3.forward
            {
                direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            }

            Vector3 position = target.position + direction * radius;

            // Calculate rotation to face outward
            Quaternion rotation = Quaternion.LookRotation(position - target.position, axis);

            // Apply additional individual rotation perpendicular to the axis
            Quaternion additionalRotation = Quaternion.AngleAxis(individualRotation, Vector3.Cross(Vector3.up, axis));

            GameObject clone = Instantiate(prefab, position, rotation * additionalRotation);
            clones.Add(clone);
            Destroy(clone, 0.05f);

            // Assign a random trigger at spawn
            Animator animator = clone.GetComponent<Animator>();
            int initialTrigger = Random.Range(1, 5);
            animator.SetTrigger(initialTrigger.ToString());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        print("Collided");
        StartCoroutine(gameObject.transform.parent.GetComponent<CarController>().SpeedBoost());
        var collisionPoint = other.ClosestPoint(transform.position);
        var temp = new GameObject(name);
        temp.transform.parent = transform;
        temp.transform.position = collisionPoint;
        target = temp.transform;
        CreateClones();
        fxObject = Instantiate(SpeedUpEffect, transform.parent.transform);
        fxObject.transform.localPosition = new Vector3(0, 0, 100);
        Destroy(fxObject, 3f);
        Destroy(temp,0.1f);
     

    }
}
