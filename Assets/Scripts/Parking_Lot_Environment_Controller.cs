using System;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public class Parking_Lot_Environment_Controller : MonoBehaviour
{
    public GameObject CarAgentPrefab;

    //hier bitte noch den Parent entsprechend festlegen
    public Transform parentToCar;

    [Header("Environment Objects")]
    //nicht sicher ob ich das alles brauche
    private Bounds boundsStreet;
    private Bounds boundsDriveway;
    private Bounds boundsParkingLot;

    public List<Transform> parkingSpaceTransforms;
    private List <ParkingSpace> parkingSpaces;

    private List <GameObject> vehicles;

    // Start is called before the first frame update
    void Start()
    {
        foreach (var parkingSpaceTransform in parkingSpaceTransforms)
        {
            //hier mal testen was passiert
            ParkingSpace.OrientationEnum ori =  (int)parkingSpaceTransform.localRotation.y == 0 ? ParkingSpace.OrientationEnum.Parallel : ParkingSpace.OrientationEnum.Perpendicular;
            parkingSpaces.Add(new ParkingSpace(false, parkingSpaceTransform.position, ori));
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShuffleOccupiedParkingSpaces()
    {
        bool[] mapping = RandomMapVehiclesToParkingSpaces();
        int indexVeh = 0;
        
        for (int i = 0; i < mapping.Length; i++)
        {
            if (mapping[i] == true)
            {
                vehicles[indexVeh].transform.rotation = Quaternion.Euler(0,parkingSpaces[i].getRotation(),0);
                vehicles[indexVeh].transform.position = parkingSpaces[i].Center;
                parkingSpaces[i].IsOccupied = true;

            }
            else
                parkingSpaces[i].IsOccupied = false;
        }
    }

    public void SetParkingLotVehicles(int p_amount)
    {
        int amount = p_amount <= parkingSpaces.Count ? p_amount : parkingSpaces.Count;
        
        if (amount == vehicles.Count)
            return;
        else if (amount < vehicles.Count)
        {
            for (int i = vehicles.Count-1; i > amount-1; i--)
            {
                Destroy(vehicles[i]);
                vehicles.RemoveAt(i);
            }
        }
        else if (amount > vehicles.Count)
        {
            for (int i = 0; i < amount; i++)
                vehicles.Add(Instantiate(CarAgentPrefab,parentToCar));
        }
    }

    private bool[] RandomMapVehiclesToParkingSpaces()
    {
        bool[] mapping = new bool[parkingSpaces.Count];
        for(int i = 0; i < vehicles.Count; i++)
        {
            int index =  (int)UnityEngine.Random.Range(0, parkingSpaces.Count);
            if (mapping[index] == false)
                mapping[index] = true;
            else
                i--;
        }
        return mapping;
    }
}

// -falls noch zeit ist können reservierte plätze zusätzlich implementiert werden
// -die orientierung kann auch noch angepasst werden, so dass die blickrichtung auch 
//  wichtig ist
class ParkingSpace
{
    public enum OrientationEnum
    {
        Perpendicular,
        Parallel
    }
    public enum SpaceTypeEnum
    {
        Regular,
        Handicapped,
        Family
    }

    public ParkingSpace(bool p_isOccupied, Vector3 p_center, OrientationEnum p_orientation, SpaceTypeEnum p_spaceType = SpaceTypeEnum.Regular)
    {
        isOccupied  = p_isOccupied;
        center      = p_center;
        orientation = p_orientation;
        spaceType   = p_spaceType;
    }

    private bool    isOccupied;
    private Vector3 center;
    private OrientationEnum orientation;
    private SpaceTypeEnum spaceType;

    public bool IsOccupied{set{isOccupied = value;}
                           get{return isOccupied;}}
    public Vector3 Center{get{return center;}}
    public OrientationEnum Orientation{get{return orientation;}}
    public SpaceTypeEnum SpaceType{get{return spaceType;}
                                   set{spaceType = value;}}

    public int getRotation()
    {
        return orientation == OrientationEnum.Parallel ? 0 : 90;
    }

}