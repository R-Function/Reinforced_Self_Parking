// using System;
// using System.Collections.Generic;
// using Unity.MLAgents;
// using Unity.MLAgents.Actuators;
// using Unity.MLAgents.Sensors;
// using Unity.VisualScripting;
// using UnityEngine;

// public class OrderedCrossingAgent : Agent
// {
//     private Rigidbody rBody;

//     [Header("Rotation")]
//     [Range(1f, 20.0f)]
//     public float torque;
//     [Range(1f, 36f)]
//     public float maxAngVel;
//     public bool enableSensorRotation;
//     [Range(1f, 5.0f)]
//     public float sensorRoationSpeed = 2f;
    
//     [Header("Speed")]
//     public int maxSpeed;
//     public float maxAcc;
//     [Range(0.001f, 1.0f)]
//     public float leteralDrag;
//     [Range(0.001f, 1.0f)]
//     public float frontDrag;

//     private List<Transform> goals;
//     private Transform bridge;
//     private Transform raycastSensor;
//     private OrderedCrossingController envController;
//     [SerializeField]
//     private bool inGoal;
//     [SerializeField]
//     private bool isRunning;
//     public bool physicsControll;


//     public override void Initialize()
//     {
//         envController = GetComponentInParent<OrderedCrossingController>();
//         rBody = GetComponent<Rigidbody>();
//         isRunning = true;
//         raycastSensor = this.transform.Find("BlickSensor");
//     }

//     // fixedUpdate um die steuerung des Panzers realistischer zu machen
//     void FixedUpdate()
//     {
//         float maxSpeedUnit      = maxSpeed / 3.6f;
//         var currentVelocity     = rBody.velocity;
//         var lateralVelocity = Vector3.Dot(currentVelocity, transform.right) * transform.right;

//         // Fahrzeug am driften hindern
//         Vector3 lateralDragForce = -lateralVelocity * leteralDrag;
//         rBody.AddForce(lateralDragForce, ForceMode.Acceleration);

//         if(rBody.angularVelocity.y >= maxAngVel || rBody.angularVelocity.y <= -maxAngVel)
//         {
//             rBody.angularVelocity = rBody.angularVelocity.normalized * maxAngVel;
//         }

//         //hindert fahrzeug daran die maximalgeschwindigkeit zu überschreiten
//         if(currentVelocity.magnitude >= maxSpeedUnit || currentVelocity.magnitude <= -maxSpeedUnit)
//         {
//             rBody.velocity = currentVelocity.normalized * maxSpeedUnit;
//         }
//     }

//     public override void CollectObservations(VectorSensor sensor)
//     {
//         foreach (Transform goal in goals)
//         {
//             sensor.AddObservation(NormalizePosition(goal.position).x);
//             sensor.AddObservation(NormalizePosition(goal.position).z);
//         }
//         sensor.AddObservation(NormalizePosition(bridge.position));
//         if (enableSensorRotation)
//             sensor.AddObservation(NormalizeRotation(raycastSensor.transform.eulerAngles).y);
//         sensor.AddObservation(isRunning);
//         // hier verändern wenn sich die Größe der Karte ändert
//         // sensor.AddObservation(NormalizeValue(bridge.localScale.x, 1, envController.bridge.localScale.x));
//         //aktivieren wenn model 3 ausgeführt wird
//         // sensor.AddObservation(NormalizeRotation(this.transform.eulerAngles));
//         sensor.AddObservation(NormalizeRotation(this.transform.eulerAngles).y);
//         sensor.AddObservation(NormalizePosition(this.transform.position));
//     }

//     public override void OnActionReceived(ActionBuffers actions)
//     {   
//         if(isRunning)
//         {
            
//             float turn  = (float)Math.Clamp(actions.ContinuousActions[0], -1f,1f);
//             float gas = (float)Math.Clamp(actions.ContinuousActions[1], -1f,1f);            
//             float sensorRot;
            

//             if(physicsControll)
//             {
//                 // rotation
//                 this.rBody.AddTorque(turn * torque * transform.up, mode: ForceMode.Acceleration);
//                 // bewegung
//                 this.rBody.AddForce(gas * maxAcc * transform.forward, mode: ForceMode.Acceleration);
//             }
//             else
//             {
//                 float rotation  = turn * 90;
//                 float speed     = gas * maxSpeed;

//                 // rotation
//                 Quaternion target = Quaternion.Euler(0, rotation + this.transform.eulerAngles.y, 0);
//                 this.transform.rotation = Quaternion.Slerp(this.transform.rotation, target, Time.deltaTime * maxAngVel);
//                 // bewegung
//                 this.transform.position += speed * Time.deltaTime * this.transform.forward;
//             }

//             //sensor roation
//             if(enableSensorRotation)
//             {
//                 sensorRot = (float)Math.Clamp(actions.ContinuousActions[2], -1f,1f) * 90;
//                 Quaternion sensorTargetRot = Quaternion.Euler(0, sensorRot + raycastSensor.transform.eulerAngles.y, 0);
//                 raycastSensor.transform.rotation = Quaternion.Slerp(raycastSensor.transform.rotation, sensorTargetRot, Time.deltaTime * sensorRoationSpeed);
//             }

//             // vorwärts/rückwärts bewegung
//             // 
            
//         }
//     }

//     private void OnTriggerEnter(Collider col)
//     {

//         if(col.gameObject.tag == "Goal")
//         {
//             inGoal = true;
//             isRunning = false;
//             envController.EnteredGoal(this);
//         }
//         if(col.gameObject.tag == "FailState")
//         {
//             isRunning = false;
//             envController.EnterFailState(this);
//         }
//     }
//     private void OnTriggerExit(Collider col)
//     {
//         if(col.gameObject.tag == "TrainingArea")
//         {
//             Debug.Log("Der Agent: "+this.gameObject.name+" ist ausgeschieden.");
//             isRunning = false;
//             envController.ExitMap(this);
//         }
//         if(col.gameObject.tag == "goal")
//         {
//             inGoal = false;
//             envController.ExitGoal(this);
//         }
//     }

//     private void OnTriggerStay(Collider col)
//     {
//         if(col.gameObject.tag == "Goal")
//             inGoal = true;
//     }

//     private Vector3 NormalizePosition(Vector3 position)
//     {
//         Bounds mapBounds = this.transform.parent.gameObject.GetComponent<Collider>().bounds;
//         return new Vector3(NormalizeValue(position.x, mapBounds.min.x,mapBounds.max.x),
//                            NormalizeValue(position.y, mapBounds.min.y,mapBounds.max.y),
//                            NormalizeValue(position.z, mapBounds.min.z,mapBounds.max.z));
//     }
//     private Vector3 NormalizeRotation(Vector3 rotation)
//     {
//         return new Vector3(rotation.x/180, rotation.y/180, rotation.z/180);
//     }

//     private float NormalizeValue(float value, float min, float max)
//     {
//         return ((value-min)/(max-min));
//     }

//     // private void OnCollisionExit(Collision col)
//     // {
//     //     if(col.gameObject.tag == "StartGround")
//     //         envController.ExitStart(this);
//     // }

//     private void OnCollisionEnter(Collision col)
//     {
//         if(col.gameObject.tag == "Agent")
//             envController.CollisionWithCoAgent(this);
//     }


//     public Rigidbody RBody
//     {
//         set{rBody = value;}
//         get{return rBody;}
//     }
//     public List<Transform> Goals
//     {
//         set{goals = value;}
//         get{return goals;}
//     }
//     public Transform Bridge
//     {
//         set{bridge = value;}
//         get{return bridge;}
//     }
//     public bool InGoal
//     {
//         set{inGoal = value;}
//         get{return inGoal;}
//     }
//     public bool IsRunning
//     {
//         get{return isRunning;}
//         set{isRunning = value;}
//     }
//     public bool PhysicsControll
//     {
//         get{return physicsControll;}
//         set{physicsControll = value;}
//     }
//     public OrderedCrossingController EnvController
//     {
//         set{envController = value;}
//         get{return envController;}
//     }
//     public bool ActionsDisabled
//     {
//         set{isRunning = value;}
//         get{return isRunning;}
//     }


//     public override void Heuristic(in ActionBuffers actionsOut)
//     {
//         var continuousActionsOut = actionsOut.ContinuousActions;
//         //rotation agent
//         continuousActionsOut[0] = Input.GetAxis("Horizontal");
//         //movement forward backward
//         continuousActionsOut[1] = Input.GetAxis("Vertical");
        
//         //rotation sensor
//         if(enableSensorRotation)
//         {
//             if(Input.GetKey("q"))
//                 continuousActionsOut[2] = -1;
//             else if(Input.GetKey("e"))
//                 continuousActionsOut[2] = 1;
//             else
//                 continuousActionsOut[2] = 0;
//         }
//     }

//     private Vector3 componentWiseMultiplication(Vector3 a, Vector3 b)
//     {
//         return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
//     }
// }
