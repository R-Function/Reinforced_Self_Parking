using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class AgentPKWBase : Agent
{
    // controll enum
    protected enum Drive{Idle     = 0,
                         Forward  = 1,
                         Reverse  = 2,
                         Brake    = 3}

    protected enum Turn{Idle      = 0,
                        Right     = 1,
                        Left      = 2}

    public GameObject parkSensor;
    protected RayCastHandler rayParkSensor;

    // public GameObject parkSpaceSensor;
    // private RayPerceptionSensor rayData;
    
    protected Rigidbody rBody;
    protected PKW_Controller pkw;
    protected Transform pkwBody;

    // den hier einführen wenn es nicht anders geht
    // private List<Transform> freeParkingSpace;

    // [SerializeField]
    // private bool inGoal;
    [SerializeField]
    public bool isRunning;
    protected bool isInGoal;

    // public string indexName;
    protected Park_Training_Controller critic;
    protected Transform parkingLot;
    [SerializeField]
    protected bool isBreakAllowed;
    [SerializeField]
    protected bool isReverseAllowed;
    protected bool parkSpaceFound;
    protected Vector3 nearestParkSpacePos;
    protected Quaternion nearestParkSpaceRot;

    /*____________________HILFSMETHODEN______________________*/
    protected Vector3 NormalizePosition(Vector3 position)
    {
        Bounds mapBounds;
        // normalisierung auf maximalgröße von map
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
    protected Vector3 NormalizeRotation(Vector3 rotation)
    {
        return new Vector3(rotation.x/360, rotation.y/360, rotation.z/360);
        // old
        //return new Vector3(rotation.x/180, rotation.y/180, rotation.z/180);
    }
    protected float NormalizeValue(float value, float min, float max, float from = 0, float to= 1)
    {
        return ((to-from)*(value-min)/(max-min)+from);
    }
    protected Vector3 componentWiseMultiplication(Vector3 a, Vector3 b)
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
    public Transform ParkingLot{get{return parkingLot;}}
    public PKW_Controller PKW{get{return pkw;}}
    public bool IsBreakAllowed{get{return isBreakAllowed;} set{isBreakAllowed = value;}}
    public bool IsReverseAllowed{get{return isReverseAllowed;} set{isReverseAllowed = value;}}
    public bool IsInGoal{ get{ return isInGoal; } set { isInGoal = value; } }


    virtual public void resetRaySensorMem()
    {
        parkSpaceFound = false;
        nearestParkSpacePos = Vector3.zero;
        nearestParkSpaceRot = Quaternion.identity;
        rayParkSensor.clearHitObjects();
    }
    virtual public void setParkingLot(Transform parkingLotTransform)
    {
        parkingLot = parkingLotTransform;
    }
}
