using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Sensors.Reflection;
using Unity.VisualScripting;
using UnityEngine;

/*  Noziten:
*   - Wenn das ausbremsen beim Fallen ein problem sein sollte, bei ThrottleOff()
*       den Invoke ausstellen und andere lösung finden.
*   - Bitte die mapBounds bei der positionsnormalisierung im Auge behalten.
*       Hier könnte eine Dreh Transformation noch von nöten sein.
*   - Überlegen, ob der Observations und Aktionsraum weiter diskretisiert werden sollte.
*       Hier bestünde die möglichkeit winkel auf 360 und den Positionsraum auf integer
*       Werte zu beschränken. 
*/

public class AgentPKW : Agent
{
    enum Drive{Idle     = 0,
               Forward  = 1,
               Reverse  = 2,
               Brake    = 3}

    enum Turn{Idle      = 0,
              Right     = 1,
              Left      = 2}

    public GameObject parkSensor;
    private RayCastHandler rayParkSensor;

    // public GameObject parkSpaceSensor;
    // private RayPerceptionSensor rayData;
    
    private Rigidbody rBody;
    private PKW_Controller pkw;
    private Transform pkwBody;

    // den hier einführen wenn es nicht anders geht
    // private List<Transform> freeParkingSpace;

    // [SerializeField]
    // private bool inGoal;
    [SerializeField]
    public bool isRunning;

    // public string indexName;
    private Park_Training_Controller critic;
    private Transform parkingLot;
    [SerializeField]
    private bool isBreakAllowed;
    [SerializeField]
    private bool isReverseAllowed;
    private bool parkSpaceFound;
    private Vector3 nearestParkSpacePos;
    private Quaternion nearestParkSpaceRot;


    public override void Initialize()
    {
        rBody          = GetComponent<Rigidbody>();
        pkw            = GetComponent<PKW_Controller>();
        rayParkSensor  = parkSensor.GetComponent<RayCastHandler>();
        resetParkSpaceMem();

        // rayData     = parkSpaceSensor.GetComponent<RayPerceptionSensorComponent3D>().RaySensor;
        isRunning   = true;
        int i = 0;
        do
        {
            pkwBody = this.transform.GetChild(i);
            i++;
        }while(pkwBody.name != "Body");
   
    }

    void Update()
    {
        var nearTransform = rayParkSensor.NearestTransform(this.transform);     
        if(nearTransform != null)
        {
            parkSpaceFound = true;
            nearestParkSpacePos = nearTransform.position;
            nearestParkSpaceRot = nearTransform.rotation;
        }
    }

    
    
    /*____________________OBSERVATIONEN______________________*/
    
    // Geschwindigkeit/Beschleunigung
    // (Zu stacked Observations gemacht, um Gefühl für Bewegung zu verbessern)
    [Observable(numStackedObservations: 3)]
    float VelocityX{
        get { return NormalizeValue(pkw.LocalVelocityX, -pkw.maxSpeed, pkw.maxSpeed, -1, 1);}
    }
    [Observable(numStackedObservations: 3)]
    float VelocityZ{
        get { return NormalizeValue(pkw.LocalVelocityZ, -pkw.maxReverseSpeed, pkw.maxSpeed, -1, 1);}
    }
    // Gyroskop (Drehung)
    [Observable]
    float Rotation{
        get { return NormalizeRotation(this.transform.eulerAngles).y;}
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //Motor
        sensor.AddObservation(this.isRunning);
        //GPS
        sensor.AddObservation(NormalizePosition(this.transform.position).x);
        sensor.AddObservation(NormalizePosition(this.transform.position).z);
        //Gyroskop
        sensor.AddObservation(NormalizeRotation(this.transform.eulerAngles).y);
        //Parkplatzposition
        sensor.AddObservation(NormalizePosition(parkingLot.position).x);
        sensor.AddObservation(NormalizePosition(parkingLot.position).z);
        //Parkplatzsensor
        sensor.AddObservation(parkSpaceFound);
        sensor.AddObservation(NormalizePosition(nearestParkSpacePos).x);
        sensor.AddObservation(NormalizePosition(nearestParkSpacePos).z);
        sensor.AddObservation(NormalizeRotation(nearestParkSpaceRot.eulerAngles).y);
    }

    /*______________________AKTIONEN______________________*/
    public override void OnActionReceived(ActionBuffers actions)
    {   
        if(isRunning)
        {
            Drive actDrive  = (Drive)actions.DiscreteActions[0];
            // float actTurn = (float)Math.Clamp(actions.ContinuousActions[0], -1f,1f);
            Turn actTurn    = (Turn)actions.DiscreteActions[1];

            //Vorwärts und Rückwärts fahren
            switch (actDrive)
            {
                case Drive.Idle:
                    pkw.ThrottleOff();
                    break;
                case Drive.Forward:
                    pkw.GoForward();
                    break;
                case Drive.Reverse:
                    if(isReverseAllowed)
                        pkw.GoReverse();
                    break;
                case Drive.Brake:
                    if(isBreakAllowed)
                        pkw.Brakes();
                    break;
            }
            // Lenkung continuous
            // pkw.Turn(actTurn);
            // Lenkung disket
            switch(actTurn)
            {
                case Turn.Idle:
                    pkw.ResetSteeringAngle();
                    break;
                case Turn.Right:
                    pkw.Turn(1);
                    break;
                case Turn.Left:
                    pkw.Turn(-1);
                    break;
            }
        }
    }

    /*______________TRIGGER/COLLIDER METHODEN__________________*/
    private void OnTriggerEnter(Collider col)
    {
        if(col.gameObject.tag == "ParkSpace")
        {
            critic.GoalEnterParkingSpace(this, col.transform);
        }
        else if(col.gameObject.tag == "Border")
        {
            critic.ExitRoad(this);
        }
    }

    private void OnTriggerExit(Collider col)
    {
        if(col.gameObject.tag == "ParkSpace")
        {
            critic.ExitParkingSpace(this, col.transform);
        }
        else if(col.gameObject.tag == "TrainingArea")
            critic.ExitTrainingArea(this);
    }

    private void OnTriggerStay(Collider col)
    {
        
    }

    private void OnCollisionEnter(Collision col)
    {
        // Debug.Log("Kollision mit: "+col.gameObject.tag);
        if (col.gameObject.tag == "Obstacle")
        {
            critic.CollisionWithObstacle(this);
        }
    }

    /*____________________HILFSMETHODEN______________________*/
    private Vector3 NormalizePosition(Vector3 position)
    {
        Bounds mapBounds;
        // normalisierung auf maximalgröße von map
        mapBounds = new Bounds(this.transform.parent.position, new Vector3(150,30,150));
        // try{
        //     mapBounds = this.transform.parent.gameObject.GetComponent<Collider>().bounds;
        // }catch(NullReferenceException e){
        //     Debug.Log(e.Message);
        //     Debug.Log("Es existieren keine Map Bounds. Standardwerte werden genutzt!");
        //     mapBounds = new Bounds(new Vector3(0,0,0), new Vector3(100,100,100));
        // }
        Vector3 pos = new Vector3(NormalizeValue(position.x, mapBounds.min.x,mapBounds.max.x,-1,1),
                                  NormalizeValue(position.y, mapBounds.min.y,mapBounds.max.y,-1,1),
                                  NormalizeValue(position.z, mapBounds.min.z,mapBounds.max.z,-1,1));
        return pos;
    }
    private Vector3 NormalizeRotation(Vector3 rotation)
    {
        return new Vector3(rotation.x/360, rotation.y/360, rotation.z/360);
        // old
        //return new Vector3(rotation.x/180, rotation.y/180, rotation.z/180);
    }
    private float NormalizeValue(float value, float min, float max, float from = 0, float to= 1)
    {
        return ((to-from)*(value-min)/(max-min)+from);
    }
    private Vector3 componentWiseMultiplication(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x * b.x, a.y * b.y, a.z * b.z);
    }

    /*________________STEUERUNG ZUM TESTEN______________________*/
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        var discreteActionsOut   = actionsOut.DiscreteActions;

        //Lenkung
        // continuousActionsOut[0] = Input.GetAxis("Horizontal");
        switch ((int)Input.GetAxis("Horizontal"))
        {
            case 0:
                discreteActionsOut[1] = 0;
                break;
            case 1:
                discreteActionsOut[1] = 1;
                break;
            case -1:
                discreteActionsOut[1] = 2;
                break;
        }
        //Fahren
        switch ((int)Input.GetAxis("Vertical"))
        {
            case 0:
                discreteActionsOut[0] = 0;
                break;
            case 1:
                discreteActionsOut[0] = 1;
                break;
            case -1:
                discreteActionsOut[0] = 2;
                break;
        }
        //Bremsen
        if(Input.GetKey("space"))
            discreteActionsOut[0]   = 3;
    }

    /*_____________________Properties________________________*/
    public Park_Training_Controller Critic{get{return critic;}set{critic = value;}}
    public Rigidbody RBody{get{return rBody;}set{rBody = value;}}
    public Transform PKWBody{get{return pkwBody;}}
    public Transform ParkingLot{get{return parkingLot;} set{parkingLot = value;}}
    public PKW_Controller PKW{get{return pkw;}}
    public bool IsBreakAllowed{get{return isBreakAllowed;} set{isBreakAllowed = value;}}
    public bool IsReverseAllowed{get{return isReverseAllowed;} set{isReverseAllowed = value;}}

    public void resetParkSpaceMem()
    {
        parkSpaceFound = false;
        nearestParkSpacePos = Vector3.zero;
        nearestParkSpaceRot = Quaternion.identity;
        rayParkSensor.clearHitObjects();
    }
}