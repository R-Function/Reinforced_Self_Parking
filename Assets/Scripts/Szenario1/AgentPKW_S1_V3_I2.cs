using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Sensors.Reflection;
using UnityEngine;

public class AgentPKW_S1_V3_I2 : AgentPKWBase
{
   private float[] nearestParkSpaceArrayDis;
    private Vector3[] nearestParkSpaceArrayDir;
    private Quaternion[] nearestParkSpaceArrayRot;
    private bool[] foundViableParkSpace;
    private bool parkSpaceSensorHit;


    private int parkSensorSize;

    public override void Initialize()
    {
        rBody          = GetComponent<Rigidbody>();
        pkw            = GetComponent<PKW_Controller>();
        rayParkSensor  = parkSensor.GetComponent<RayCastHandler>();
        parkSpaceSensorHit = false;

        parkSensorSize = rayParkSensor.nearestObjectsListSize;
        nearestParkSpaceArrayDis = new float[parkSensorSize];
        nearestParkSpaceArrayRot = new Quaternion[parkSensorSize];
        nearestParkSpaceArrayDir = new Vector3[parkSensorSize];
        foundViableParkSpace     = new bool[parkSensorSize];

        resetRaySensorMem();

        isRunning   = true;
        pkwBody = this.transform.Find("Body");   
    }

    void Update()
    {
        CheckForParkSpace();
    }


    /*____________________OBSERVATIONEN______________________*/
    
    // Geschwindigkeit/Beschleunigung
    // (Zu stacked Observations gemacht, um Gefühl für Bewegung zu verbessern)
    [Observable(numStackedObservations: 1)]
    float CarSpeed{
        get { return NormalizeValue(pkw.carSpeed, -pkw.maxSpeed, pkw.maxSpeed, -1, 1);}
    }
    //Radeinschlagwinkelsensor
    [Observable(numStackedObservations: 5)]
    float SteerAngle{
        get {return NormalizeValue(pkw.frontLeftCollider.steerAngle, -pkw.maxSteeringAngle,pkw.maxSteeringAngle,-1,1);}
    }
    // Gyroskop (Drehung)
    [Observable]
    float CarRotation{
        get {return NormalizeValue((int)this.transform.eulerAngles.y,0,360);}
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // //In Goal check
        // sensor.AddObservation(isInGoal);
        //Motor
        sensor.AddObservation(this.isRunning);
        //GPS
        sensor.AddObservation(NormalizePosition(this.transform.position).x);
        sensor.AddObservation(NormalizePosition(this.transform.position).z);
        //Parkplatzposition
        sensor.AddObservation(NormalizePosition(parkingLot.position).x);
        sensor.AddObservation(NormalizePosition(parkingLot.position).z);
        //Parkplatzsensor
        sensor.AddObservation(parkSpaceSensorHit);
        for(int i = 0; i < parkSensorSize; i++)
        {
            sensor.AddObservation(foundViableParkSpace[i]);
            sensor.AddObservation(System.Math.Clamp(NormalizeValue(nearestParkSpaceArrayDis[i], 0, rayParkSensor.rayDistance),0f,1f));
            sensor.AddObservation(nearestParkSpaceArrayDir[i].x);
            sensor.AddObservation(nearestParkSpaceArrayDir[i].z);
            sensor.AddObservation(NormalizeValue((int)nearestParkSpaceArrayRot[i].eulerAngles.y,0,360));
        }
    }


    // wenn mindestens ein freier Parkplatz gefunden wurde
    // wird dieser in die Liste derzeit naheliegender,
    // möglicher Parkplätze aufgenommen
    private void CheckForParkSpace()
    {
        //check for Park Spaces
        var nearTransform = rayParkSensor.NearestTransform(this.transform);
        if(nearTransform != null && nearTransform.Count > 0)
        {
            int i = 0;
            foreach(var nearParkSpace in nearTransform)
            {
                parkSpaceSensorHit = true;
                if(nearParkSpace != null)
                {
                    if(!critic.IsParkSpaceOccupied(nearParkSpace))
                        foundViableParkSpace[i] = true;
                    else
                        foundViableParkSpace[i] = false;
                    nearestParkSpaceArrayDir[i] = (nearParkSpace.position - this.transform.position).normalized;
                    nearestParkSpaceArrayDis[i] = Vector3.Distance(this.transform.position, nearParkSpace.position);
                    nearestParkSpaceArrayRot[i] = nearParkSpace.rotation;
                    i++;
                }
            }
        }
        else
        {
            parkSpaceSensorHit = false;
            for(int i = 0; i < parkSensorSize; i++)
            {
               SetNearParkSpaceZero(i);
            }
        }
        // Testen der Werte
        // for(int i = 0; i < parkSensorSize; i++)
        //     {
        //         Debug.Log("Is viable Parkspace"+ i + ".: "+foundViableParkSpace[i]);
        //         Debug.Log("Hit norm Direction "+ i + ".: " +nearestParkSpaceArrayDir[i]);
        //         Debug.Log("Hit norm Distance " + i + ".: " + System.Math.Clamp(NormalizeValue(nearestParkSpaceArrayDis[i], 0, rayParkSensor.rayDistance),0,1));
        //         Debug.Log("Hit norm rotation " + i + ".:" + NormalizeValue((int)nearestParkSpaceArrayRot[i].eulerAngles.y,0,360));
        //     }
    }
    private void SetNearParkSpaceZero(int index)
    {
        foundViableParkSpace[index]     = false;
        nearestParkSpaceArrayDir[index] = Vector3.zero;
        nearestParkSpaceArrayDis[index] = 0f;
        nearestParkSpaceArrayRot[index] = Quaternion.identity;
    }

    /*______________________AKTIONEN______________________*/
    public override void OnActionReceived(ActionBuffers actions)
    {   
        if(isRunning)
        {
            Drive actDrive  = (Drive)actions.DiscreteActions[0];
            // float actTurn = (float)System.Math.Clamp(actions.ContinuousActions[0], -1f,1f);
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
        else
        {
            pkw.ThrottleOff();
            pkw.ResetSteeringAngle();
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
        else if(col.gameObject.tag == "FailState")
            critic.ExitTrainingArea(this);
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
        else if (col.gameObject.tag == "Agent")
        {
            critic.CollisionWithAgent(this);
        }
        else if(col.gameObject.tag == "ParkedCar")
        {
            critic.CollisionWithParkedCar(this);
        }
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
    /*___________Hilfmethoden___________*/
    public override void resetRaySensorMem()
    {
        parkSpaceFound = false;
        rayParkSensor.clearHitObjects();
    }
}
