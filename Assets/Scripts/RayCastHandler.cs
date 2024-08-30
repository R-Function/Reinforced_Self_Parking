using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class RayCastHandler : MonoBehaviour
{
    public LayerMask detectableLayers; // Layers the ray can interact with
    public string detectableTag;
    
    [Range(0, 90)]
    public int rayDistance = 100; // Distance the ray will check for collisions
    [Range(0, 360)]
    public float coneAngle = 10;
    [Range(0, 25)]
    public int numberOfRays = 5;
    [Range(0, 10)]
    public int nearestObjectsListSize = 1;

    
    private Vector3[] directions;
    private Quaternion lastRotation;
    private HashSet<Transform> hitObjects;
    private HashSet<Transform> nearestObjects;

    void Start()
    {
        CalculateDirections();
        hitObjects = new HashSet<Transform>();
        nearestObjects = new HashSet<Transform>();
        
        if(nearestObjectsListSize > numberOfRays)
        {
            Debug.Log("Size for nearest Objects List is bigger than the number of Rays cast. Value will be set to NumberOfRays.");
            nearestObjectsListSize = numberOfRays;
        }
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
        hitObjects = CastRays();
    }

    public HashSet<Transform> NearestTransform(Transform agent)
    {
        nearestObjects.Clear();
        // wenn objekte in liste aufgenommen wurden
        if(hitObjects.Count != 0)
        {
            // nur die vorgegebene Anzahl an nächstgelegenen Objekten weitergeben
            for(int i = 0; i < nearestObjectsListSize; i++)
            {
                Transform nearest = null;
                // suche die n naheliegensten getroffenen Objekte aus der Liste
                foreach(Transform t in hitObjects)
                {
                    if(!nearestObjects.Contains(t) && (nearest == null || Vector3.Distance(agent.position, t.position) < Vector3.Distance(agent.position, nearest.position)))
                        nearest = t;
                }
                // // Output the name of the object hit
                // Debug.Log("Hit object: " + nearest.gameObject.name);

                // // Additional information you can retrieve
                // if(nearest != null)
                // {
                //     Vector3 hitObjectPosition = nearest.position;
                //     float hitObjectRotation   = nearest.eulerAngles.y;

                //     // // Example: Log the hit point coordinates
                //     Debug.Log("Hit position: " + hitObjectPosition);
                //     Debug.Log("Hit rotation: " + hitObjectRotation);
                // }

                nearestObjects.Add(nearest);
            }
            return nearestObjects;
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

    private HashSet<Transform> CastRays()
    {
        Vector3 startPoint = this.transform.position;
        HashSet<Transform> currentHitObjects = new HashSet<Transform>();
        
        foreach(Vector3 direction in this.directions)
        {
            // Define the ray starting point and direction
            Ray ray = new Ray(startPoint, direction);
            RaycastHit hit;

            // Perform the raycast
            if (Physics.Raycast(ray, out hit, rayDistance, detectableLayers))
            {
                if(!currentHitObjects.Contains(hit.transform) && hit.collider.tag == detectableTag)
                {
                    currentHitObjects.Add(hit.transform);
                }
            }
        }
        return currentHitObjects;
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
