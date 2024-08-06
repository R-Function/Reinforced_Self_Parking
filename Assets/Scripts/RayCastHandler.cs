using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RayCastHandler : MonoBehaviour
{
    public LayerMask detectableLayers; // Layers the ray can interact with
    public string detectableTag;
    
    [Range(0, 90)]
    public int rayDistance = 100; // Distance the ray will check for collisions
    [Range(0, 360)]
    public float coneAngle = 10;
    [Range(0,10)]
    public int numberOfRays = 5;

    
    private Vector3[] directions;
    private Quaternion lastRotation;
    private HashSet<Transform> hitObjects;

    void Start()
    {
        CalculateDirections();
        hitObjects = new HashSet<Transform>();
    }

    void Update()
    {
        lastRotation = transform.rotation;
        // Check if the rotation has changed
        if (transform.rotation != lastRotation)
        {
            CalculateDirections(); // Recalculate directions on rotation change
            lastRotation = transform.rotation; // Update lastRotation
        }
        CastRays();
    }

    public Transform NearestTransform(Transform agent)
    {
        // wenn objekte in liste aufgenommen wurden
        if(hitObjects.Count != 0)
        {
            Transform nearest = null;
            foreach(Transform t in hitObjects)
            {
                if(nearest == null ||
                Vector3.Distance(agent.position, t.position) < Vector3.Distance(agent.position, nearest.position))
                    nearest = t;
            }
            // // Output the name of the object hit
            // Debug.Log("Hit object: " + nearest.gameObject.name);

            // // Additional information you can retrieve
            // Vector3 hitObjectPosition = nearest.position;
            // float hitObjectRotation   = nearest.eulerAngles.y;

            // // Example: Log the hit point coordinates
            // Debug.Log("Hit position: " + hitObjectPosition);
            // Debug.Log("Hit rotation: " + hitObjectRotation);

            return nearest;
        }
        else
        {
            // Debug.Log("No objects with the tag: "+detectableTag+" found yet.");
            return null;
        }
    }

    public void clearHitObjects()
    {
        if(hitObjects != null)
            this.hitObjects.Clear();
    }

    private void CalculateDirections()
    {
        directions = new Vector3[numberOfRays];
        float halfConeAngle = coneAngle / 2f;
        float stepAngle = coneAngle / (numberOfRays - 1);

        for (int i = 0; i < numberOfRays; i++)
        {
            float angle = -halfConeAngle + (stepAngle * i);
            float radian = angle * Mathf.Deg2Rad;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * transform.forward;
            directions[i] = direction;
        }
    }

    private void CastRays()
    {
        Vector3 startPoint = this.transform.position;
        
        foreach(Vector3 direction in this.directions)
        {
            // Define the ray starting point and direction
            Ray ray = new Ray(startPoint, direction);
            RaycastHit hit;

            // Perform the raycast
            if (Physics.Raycast(ray, out hit, rayDistance, detectableLayers))
            {
                if(!hitObjects.Contains(hit.transform) && hit.collider.tag == detectableTag)
                {
                    hitObjects.Add(hit.transform);
                }
            }
        }
    }


    void OnDrawGizmosSelected()
    {
        CalculateDirections();

        // Define the starting point of the rays
        Vector3 startPoint = transform.position;

        // Draw each ray as a Gizmo line
        Gizmos.color = Color.white;
        foreach (Vector3 direction in directions)
        {
            Ray ray = new Ray(startPoint, direction);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, rayDistance, detectableLayers))
            {
                // Set Gizmo color to green if hit
                Gizmos.color = Color.green;
                Gizmos.DrawLine(startPoint, hit.point);
            }
            else
            {
                // Set Gizmo color to red if no hit
                Gizmos.color = Color.white;
                Gizmos.DrawLine(startPoint, startPoint + direction * rayDistance);
            }
        }
    }

    void OnValidate()
    {
        CalculateDirections();
    }
}
