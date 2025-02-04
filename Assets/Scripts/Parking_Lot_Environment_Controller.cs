using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
    - Die benennung der Parklücken abändern, so dass klarer ist, was gemeint wird.
*/
public class Parking_Lot_Environment_Controller : MonoBehaviour
{
    private const float TILE_SPACING = 7.5f;

    //Testvariablen
    // private int parkedCarCount = 10;
    // private float randomiseCounter = 0;
    // private float randomiseAt = 3;


    public GameObject carObstaclePrefab;

    private Transform carObstacleContainer;

    public List<Transform> spawnPoints;
    public Transform parkingSpaceTransforms;

    private Dictionary<Transform,bool> parkingSpaces;
    private List <GameObject> vehicles;
    private List <Transform> dynamicOccupiedParkSpaces;
    
    private Transform[,] tileMatrix;

    // Start is called before the first frame update
    void Awake()
    {
        parkingSpaces   = new Dictionary<Transform, bool>();
        vehicles        = new List<GameObject>();
        //tileMatrix      = st_createTileMatrix();
        dynamicOccupiedParkSpaces = new List<Transform>();

        // lade die parkplätze in die parkplatzliste
        int index = 0;
        foreach (Transform parkingSpaceTransform in parkingSpaceTransforms)
        {
            foreach(Transform child in parkingSpaceTransform)
            {
                if(child.tag == "ParkSpace")
                {
                    parkingSpaces.Add(child, false);
                    // Debug.Log("Hinzufügen des Parkplatzes zur Liste: "+child.tag+index.ToString());
                    index++;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // randomiseCounter += Time.deltaTime;
        // if(randomiseCounter > randomiseAt)
        // {
        //     ZLineShuffle(1,4,0,3);
        //     SetAndShuffleCars(parkedCarCount);
        //     randomiseCounter = 0f;
        // } 
    }

    public bool IsParkSpaceOccupied(Transform parkSpace)
    {
        return parkingSpaces[parkSpace];
    }

    public void SetParkSpaceOccupied(Transform parkSpace)
    {
        dynamicOccupiedParkSpaces.Add(parkSpace);
        parkingSpaces[parkSpace] = true;
    }

    public void ClearOccupiedParkSpaces()
    {
        foreach(Transform pSpace in dynamicOccupiedParkSpaces)
        {
            parkingSpaces[pSpace] = false;
        }
        dynamicOccupiedParkSpaces.Clear();
    }

    // vertauscht zufällig zwei Tile-Reihen mit der länge depth 
    // im Z-bereich von min bis max 
    public void ZLineShuffle(int col_from, int col_to, int row_from, int row_to)
    {
        int column_a = UnityEngine.Random.Range(col_from, col_to+1);
        int column_b = RandomRangeExcept(col_from, col_to, column_a);
        Transform tile_buffer;
        // Debug.Log("ColA: "+column_a.ToString()+" ColB: "+column_b.ToString());

        for(int row = row_from; row <= row_to; row++)
        {

            if(this.tileMatrix[row,column_a] != null)
            {
                //position tauschen
                this.tileMatrix[row, column_a].localPosition = new Vector3(row*TILE_SPACING, 0, column_b*TILE_SPACING);
                
            }
            else{}
                // Debug.Log("No Tile at: ["+row.ToString()+","+column_a+"]");
            if(this.tileMatrix[row,column_b] != null)
            {
                //position tauschen
                this.tileMatrix[row, column_b].localPosition = new Vector3(row*TILE_SPACING, 0, column_a*TILE_SPACING);
            }
            else{}
                // Debug.Log("No Tile at: ["+row.ToString()+","+column_b+"]");

            //wenn beide existieren dann mit zwischenspeichern
            if(this.tileMatrix[row,column_a] != null && this.tileMatrix[row,column_b] != null)
            {
                tile_buffer = this.tileMatrix[row, column_a];
                this.tileMatrix[row, column_a] = this.tileMatrix[row, column_b];
                this.tileMatrix[row, column_b] = tile_buffer;
            }
            //referenz in der matrix tauschen wenn b = null
            else if(this.tileMatrix[row,column_a] != null && this.tileMatrix[row,column_b] == null)
            {
                this.tileMatrix[row, column_b] = this.tileMatrix[row, column_a];
                this.tileMatrix[row, column_a] = null;
            }
            //referenz in der matrix tauschen wenn a = null
            else if(this.tileMatrix[row,column_a] == null && this.tileMatrix[row,column_b] != null)
            {
                this.tileMatrix[row, column_a] = this.tileMatrix[row, column_b];
                this.tileMatrix[row, column_b] = null;
            }
        }
    }

    private Transform[,] st_createTileMatrix()
    {
        int ind_x;
        int ind_z;
        int x_max = 0;
        int z_max = 0;
        
        // größenbestimmung
        foreach(Transform tile in this.transform)
        {
            ind_x = (int) (tile.localPosition.x / TILE_SPACING);
            ind_z = (int) (tile.localPosition.z / TILE_SPACING);

            //wenn der index größer ist als das derzeitige
            //maximum, dann setze das maximum auf den index
            x_max = ind_x >= x_max ? ind_x : x_max;
            z_max = ind_z >= z_max ? ind_z : z_max;
        }

        Transform[,] resultTileMatrix = new Transform[x_max+1,z_max+1];

        foreach(Transform tile in this.transform)
        {
            ind_x = (int) (tile.localPosition.x / TILE_SPACING);
            ind_z = (int) (tile.localPosition.z / TILE_SPACING);

            //Debug.Log("!!!"+ind_x+","+ind_z+"!!!");

            resultTileMatrix[ind_x,ind_z] = tile;
        }

        return resultTileMatrix;
    }

    //stellt sich, dass keine autos gespawned werden, denen kein
    //parkplatz zugewiesen wurde
    public void SetAndShuffleCars(int p_amount)
    {
        SetParkingLotVehicles(p_amount);
        ShuffleOccupiedParkingSpaces();
    }

    public void ShuffleOccupiedParkingSpaces()
    {
        bool[] mapping = RandomMapVehiclesToParkingSpaces();
        int indexVeh = 0;
        
        for (int i = 0; i < mapping.Length; i++)
        {
            if (mapping[i] == true)
            {
                vehicles[indexVeh].transform.localRotation = Quaternion.Euler(0,parkingSpaces.ElementAt(i).Key.rotation.eulerAngles.y,0);
                vehicles[indexVeh].transform.position = parkingSpaces.ElementAt(i).Key.position;
                parkingSpaces[parkingSpaces.ElementAt(i).Key] = true;
                indexVeh++;
            }
            else
                parkingSpaces[parkingSpaces.ElementAt(i).Key] = false;
        }
    }

    private void SetParkingLotVehicles(int p_amount)
    {
        //setze werte unter 0 auf 0
        p_amount = p_amount < 0 ? 0 : p_amount;
        //setze werte, die größer sind als die anzahl verfügbarer plätze
        //auf die anzahl verfügbarer plätze
        int amount  = p_amount <= parkingSpaces.Count ? p_amount : parkingSpaces.Count;
        
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
                vehicles.Add(Instantiate(carObstaclePrefab,carObstacleContainer));
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

    private int RandomRangeExcept (int min, int max, int except) 
    {
        int number;
        do 
        {
            number = UnityEngine.Random.Range(min, max);
        }while (number == except);
        return number;
    }

    //_________________properties_________________________
    public Transform CarObstacleContainer{ get{return carObstacleContainer;} set{carObstacleContainer = value;}}
}