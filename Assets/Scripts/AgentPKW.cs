using System;
using Unity.MLAgents;
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

public class AgentPKW : Agent
{
    enum Drive{Idle     = 0,
               Forward  = 1,
               Reverse  = 2}

    private Rigidbody rBody;
    private PKW_Controller pkw;

    // den hier einführen wenn es nicht anders geht
    // private List<Transform> freeParkingSpace;

    [SerializeField]
    private bool inGoal;
    [SerializeField]
    public bool isRunning;

    private Park_Training_Controller critic;

    public override void Initialize()
    {
        rBody     = GetComponent<Rigidbody>();
        pkw       = GetComponent<PKW_Controller>();
        isRunning = true;
    }
    
    /*____________________OBSERVATIONEN______________________*/
    
    // Geschwindigkeit/Beschleunigung
    // (Zu stacked Observations gemacht, um Gefühl für Bewegung zu verbessern)
    [Observable(numStackedObservations: 4)]
    float VelocityX{
        get { return NormalizeValue(pkw.LocalVelocityX, -pkw.maxSpeed, pkw.maxSpeed, -1, 1);}
    }
    [Observable(numStackedObservations: 4)]
    float VelocityZ{
        get { return NormalizeValue(pkw.LocalVelocityZ, -pkw.maxReverseSpeed, pkw.maxSpeed, -1, 1);}
    }
    // Gyroskop (Drehung)
    [Observable(numStackedObservations: 4)]
    float Rotation{
        get { return NormalizeRotation(this.transform.eulerAngles).y;}
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //GPS
        sensor.AddObservation(NormalizePosition(this.transform.position).x);
        sensor.AddObservation(NormalizePosition(this.transform.position).z);
        //Gyroskop
        sensor.AddObservation(NormalizeRotation(this.transform.eulerAngles).y);
    }

    /*______________________AKTIONEN______________________*/
    public override void OnActionReceived(ActionBuffers actions)
    {   
        if(isRunning)
        {
            Drive actDrive  = (Drive)actions.DiscreteActions[0];
            int actBrake  = actions.DiscreteActions[1];
            float actTurn = (float)Math.Clamp(actions.ContinuousActions[0], -1f,1f);

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
                    pkw.GoReverse();
                    break;
            }
            //Lenkung
            pkw.Turn(actTurn);
            //Bremse
            if(actBrake == 1)
                pkw.Brakes();
        }
    }

    /*______________TRIGGER/COLLIDER METHODEN__________________*/
    private void OnTriggerEnter(Collider col)
    {
        
    }

    private void OnTriggerExit(Collider col)
    {
        
    }

    private void OnTriggerStay(Collider col)
    {
        
    }

    private void OnCollisionEnter(Collision col)
    {
        
    }

    /*____________________HILFSMETHODEN______________________*/
    private Vector3 NormalizePosition(Vector3 position)
    {
        Bounds mapBounds;
        try{
            mapBounds = this.transform.parent.gameObject.GetComponent<Collider>().bounds;
        }catch(NullReferenceException e){
            Debug.Log(e.Message);
            Debug.Log("Es existieren keine Map Bounds. Standardwerte werden genutzt!");
            mapBounds = new Bounds(new Vector3(0,0,0), new Vector3(100,100,100));
        }
        Vector3 pos = new Vector3(NormalizeValue(position.x, mapBounds.min.x,mapBounds.max.x,-1,1),
                                  NormalizeValue(position.y, mapBounds.min.y,mapBounds.max.y,-1,1),
                                  NormalizeValue(position.z, mapBounds.min.z,mapBounds.max.z,-1,1));
        return pos;
    }
    private Vector3 NormalizeRotation(Vector3 rotation)
    {
        return new Vector3(rotation.x/180, rotation.y/180, rotation.z/180);
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
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
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
        discreteActionsOut[1]   = Input.GetKey("space")? 1 : 0;
    }

    /*_____________________Attribute________________________*/
    public Park_Training_Controller Critic{get{return critic;}set{critic = value;}}
}