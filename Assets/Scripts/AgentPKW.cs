using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Sensors.Reflection;
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

public class AgentPKW : AgentPKWBase
{    
    public override void Initialize()
    {
        rBody          = GetComponent<Rigidbody>();
        pkw            = GetComponent<PKW_Controller>();
        // rayParkSensor  = parkSensor.GetComponent<RayCastHandler>();
        // resetRaySensorMem();

        // rayData     = parkSpaceSensor.GetComponent<RayPerceptionSensorComponent3D>().RaySensor;
        isRunning   = true;
        pkwBody = this.transform.Find("Body");   
    }

    void Update()
    {
        // var nearTransform = rayParkSensor.NearestTransform(this.transform);     
        // if(nearTransform != null)
        // {
        //     parkSpaceFound = true;
        //     // nearestParkSpacePos = nearTransform.position;
        //     // nearestParkSpaceRot = nearTransform.rotation;
        // }
    }


    /*____________________OBSERVATIONEN______________________*/
    
    // Geschwindigkeit/Beschleunigung
    // (Zu stacked Observations gemacht, um Gefühl für Bewegung zu verbessern)
    [Observable(numStackedObservations: 1)]
    float VelocityZ{
        get { return NormalizeValue(pkw.LocalVelocityZ, -pkw.maxReverseSpeed, pkw.maxSpeed, -1, 1);}
    }
    // Gyroskop (Drehung)
    [Observable]
    float Rotation{
        get { return NormalizeValue((int)this.transform.eulerAngles.y,0,360);}
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //Motor
        sensor.AddObservation(this.isRunning);
        //GPS
        sensor.AddObservation(NormalizePosition(this.transform.position).x);
        sensor.AddObservation(NormalizePosition(this.transform.position).z);
        //Parkplatzposition
        sensor.AddObservation(NormalizePosition(parkingLot.position).x);
        sensor.AddObservation(NormalizePosition(parkingLot.position).z);
        //Parkplatzsensor
        // sensor.AddObservation(parkSpaceFound);
    //     sensor.AddObservation(NormalizePosition(nearestParkSpacePos).x);
    //     sensor.AddObservation(NormalizePosition(nearestParkSpacePos).z);
    //     sensor.AddObservation(NormalizeRotation(nearestParkSpaceRot.eulerAngles).y);
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
        return;
    }
}