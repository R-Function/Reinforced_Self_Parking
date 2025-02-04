using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using System;
using System.Linq;

//singleagent
public class Park_Training_Controller : MonoBehaviour
{
    //Testvariablen
    // private int parkedCarCount = 10;
    // private float randomiseCounter = 0;
    // private float randomiseAt = 3;
    private Parking_Lot_Environment_Controller envController;
    public List<GameObject> trainingEnvironmentList;
    public Transform carObstacleContainer;

    private int maxTrainingSteps;
    private int m_ResetTimer;
    private int episodeCounter = 10000;
    
    public TextAsset curriculumFile;
    private Lesson[] curriculum;
    private Lesson   currentLesson;

    //[Header("Environment Parameter")]
    private EnvironmentParameters envParameters;


    [Header("Agent List and Parameters")]
    public List<AgentPKWBase> agentList;
    private SimpleMultiAgentGroup m_AgentGroup;

    private List<Transform> spawnPoints;
    private GameObject currentEnvironment;

    private Dictionary<AgentPKWBase, AgentInfo> agentInformationList;

    [Range(0,4)]
    public int startLessonDefault = 2;

    // Start is called before the first frame update
    void Start()
    {
        // laden der Curricula
        envParameters         = Academy.Instance.EnvironmentParameters;
        string curriculumText = curriculumFile.text;
        curriculum            = JsonUtility.FromJson<Curriculum>(curriculumText).Lessons;
        currentLesson         = curriculum[(int)envParameters.GetWithDefault("PKWParking_Parameters", startLessonDefault)];

        // Umgebung Instanziieren
        maxTrainingSteps = currentLesson.maxSteps;
        setEnvironment(currentLesson.environmentPrefabName);

        // instanziieren der Listen
        agentInformationList  = new Dictionary<AgentPKWBase, AgentInfo>();
        m_AgentGroup          = new SimpleMultiAgentGroup();

        //initialisieren der Agenten
        foreach(AgentPKWBase agent in agentList)
        {
            agent.Critic     = this;
            agent.setParkingLot(envController.parkingSpaceTransforms);
            agentInformationList.Add(agent, new AgentInfo());
            m_AgentGroup.RegisterAgent(agent);
        }

        ResetScene();
    }

    void FixedUpdate()
    {
        m_ResetTimer += 1;
        foreach(AgentPKWBase a in m_AgentGroup.GetRegisteredAgents())
        {
            if(a.isRunning == true)
                a.AddReward(-1f/maxTrainingSteps);
            if(currentLesson.rewardDriveForward && a.PKW.carSpeed >= 12)
                a.AddReward(1f/maxTrainingSteps);

        }
        if (m_ResetTimer >= maxTrainingSteps && maxTrainingSteps > 0)
        {
            foreach(AgentPKWBase a in agentList)
                a.isRunning = false;
            FinishEpisode(true);
            Debug.Log("Folgendes Training hat die erlaubte Anzahl Steps überschritten: "+this.gameObject.name);
        }

        //zum testen, wenn agent auf parkplatz, dann belohnung berechnen
        // foreach(KeyValuePair<AgentPKWBase, AgentInfo> agentInfo in agentInformationList)
        // {
        //     if(agentInfo.Value.parkingSpacesInContact.Any())
        //     {
        //         var distanceReward = CalcDistanceReward(agentInfo.Key.PKWBody, agentInfo.Value.parkingSpacesInContact.Last(), 1);
        //         var rotationReward = CalcRotationReward(agentInfo.Key.PKWBody, agentInfo.Value.parkingSpacesInContact.Last(), 1);
        //         Debug.Log(agentInfo.Key.gameObject.name
        //                   +" hat folgende Belohnung:\nDistanz: "
        //                   + distanceReward.ToString() + ", Rotation: " 
        //                   + rotationReward.ToString());
        //     }
        // }
    }

    // Update is called once per frame
    void Update()
    {
        foreach(KeyValuePair<AgentPKWBase, AgentInfo> agentInfoPair in agentInformationList)
        {
            GoalStayInParkingSpace(agentInfoPair);
        }
    }

    /*#########################################################*/
    /*                     Reward Methoden                     */
    /*#########################################################*/
    public void ExitTrainingArea(AgentPKWBase agent)
    {
        agent.AddReward(-1f);
        agent.isRunning = false;
        FinishEpisode();
    }

    public void ExitRoad(AgentPKWBase agent)
    {
        agent.AddReward(-0.2f);
    }

    public void OffRoad(AgentPKWBase agent)
    {
        agent.AddReward(-0.2f/maxTrainingSteps);
    }

    public void CollisionWithAgent(AgentPKWBase agent)
    {
        agent.AddReward(-0.5f);
    }

    public void CollisionWithObstacle(AgentPKWBase agent)
    {
        agent.AddReward(-0.5f);
        if(currentLesson.agentControllReverse == false)
        {
            agent.isRunning = false;
            FinishEpisode();
        }
    }

    public void CollisionWithParkedCar(AgentPKWBase agent)
    {
        agent.AddReward(-0.3f);
        if(currentLesson.agentControllReverse == false)
        {
            agent.isRunning = false;
            FinishEpisode();
        }
    }

    public void GoalEnterParkingSpace(AgentPKWBase agent, Transform parkingSpace)
    {
        if(!IsParkSpaceOccupied(parkingSpace))
        {
            agent.IsInGoal = true;
            agentInformationList[agent].parkingSpacesInContact.Add(parkingSpace);
            agentInformationList[agent].TimeInParkingSpace = 0;
        }
    }

    private void GoalStayInParkingSpace(KeyValuePair<AgentPKWBase, AgentInfo> agentInfoPair)
    {
        // nur wenn fahrzeug eine parklücke berührt
        if(agentInfoPair.Value.parkingSpacesInContact.Any())
        {
            // wenn lange genug in parklücke gewesen,
            // --> reward austeilen, agent abschalten
            if(!IsParkSpaceOccupied(agentInfoPair.Value.parkingSpacesInContact.Last()) 
               && agentInfoPair.Value.GetTimeinSeconds() >= this.currentLesson.remainTimeInParkingSpace)
            {
                agentInfoPair.Key.isRunning = false;
                envController.SetParkSpaceOccupied(agentInfoPair.Value.parkingSpacesInContact.Last());
                float distReward = CalcDistanceReward(agentInfoPair.Key.PKWBody, agentInfoPair.Value.parkingSpacesInContact.Last(), 0.5f);
                float rotReward  = CalcRotationReward(agentInfoPair.Key.PKWBody, agentInfoPair.Value.parkingSpacesInContact.Last(), 0.5f);
                agentInfoPair.Key.AddReward(distReward + rotReward);
                FinishEpisode();
                // Debug.Log(agentInfoPair.Key.gameObject.name
                //           +" hat folgende Belohnung:\nDistanz: "
                //           + distReward.ToString() + ", Rotation: " 
                //           + rotReward.ToString());
            }
            // sonst --> warten
            else
                agentInfoPair.Value.TimeInParkingSpace += 1;
        }
    }

    public void ExitParkingSpace(AgentPKWBase agent, Transform parkingSpace)
    {
        if(!IsParkSpaceOccupied(parkingSpace))
        {
            agentInformationList[agent].parkingSpacesInContact.Remove(parkingSpace);
            agentInformationList[agent].TimeInParkingSpace = 0;
            if(agentInformationList[agent].parkingSpacesInContact.Count <= 0)
                agent.IsInGoal = false;
        }
    }

    public void GoalExitParkingLot()
    {
        /*  Ziel besteht allein darin aus dem Parkplatz 
            auf die straße und aus dem gebiet zu fahren
            so schnell es geht und ohne kollisionen.
        */
    }

    private float CalcDistanceReward(Transform agentBody, Transform goal, float baseReward = 0)
    {
        // Berechnung des maximal möglichen abstands der mittelpunkte bei collision
        float agentDiagonal = Mathf.Sqrt(Mathf.Pow(LongSide(agentBody),2f)+Mathf.Pow(ShortSide(agentBody),2f));
        float goalDiagonal  = Mathf.Sqrt(Mathf.Pow(LongSide(goal),2f)+Mathf.Pow(ShortSide(goal),2f));
        float maxDistance   = goalDiagonal + agentDiagonal;

        // Normalisierter Abstand von Agent und Ziel
        float distanceNorm = UnityEngine.Vector3.Distance(agentBody.position, goal.position)/maxDistance;
        
        // Reward wird quadratisch bestimmt, damit der mittelpunkt
        // deutlich besser belohnt wird als der rand
        float reward = Mathf.Pow((1-distanceNorm),2f) * baseReward;
        // if(reward > baseReward)
        //     Debug.Log("Dist Reward von "+agentBody.name+" ist = "+reward.ToString());
        return reward;
    }

    private float CalcRotationReward(Transform agentBody, Transform goal, float baseReward = 0)
    {
        float rotAgent  = agentBody.eulerAngles.y - 180;
        float rotGoal   = goal.eulerAngles.y - 180;
        float rotOffset = Mathf.Abs(rotGoal-rotAgent);

        // normalisiert auf einen bereich von -1 bis 1
        // --> vorwärts wird genauso gewertet wie rückwärts
        float offsetNorm = (2 * rotOffset / 360) - 1;

        // Rewardbestimmung entspricht der Distanz,
        // unterschied ist, dass die beiden Randwerte (0, 180)
        // die höchste belohnung geben sollen
        float reward = Mathf.Pow(offsetNorm, 2) * baseReward;
        // if(reward > baseReward*0.9)
        // {
        //     Debug.Log("Rot Reward von "+agentBody.name+" ist = "+reward.ToString());
        //     Debug.Log("rot Agent: "+rotAgent);
        //     Debug.Log("rot Goal: "+rotGoal);
        // }
        return reward;
    }

    //hilfmethoden
    private float LongSide(Transform t)
    {
        try
        {
            float x = t.GetComponent<Collider>().bounds.extents.x;
            float z = t.GetComponent<Collider>().bounds.extents.z; 
            return x > z ? x : z;
        }catch(Exception e)
        {
            Debug.LogError(e.Message);
            Debug.LogError("The Transform doesnt contain a Collider Component.");
            return 0f;
        }
    }

    private float ShortSide(Transform t)
    {
        try
        {
            float x = t.GetComponent<Collider>().bounds.extents.x;
            float z = t.GetComponent<Collider>().bounds.extents.z; 
            return x < z ? x : z;
        }catch(Exception)
        {
            Debug.LogError("The Transform doesnt contain a Collider Component.");
            return 0f;
        }
    }

    
    /*#########################################################*/
    /*                  Environment Methoden                   */
    /*#########################################################*/
    public void FinishEpisode(bool isInterupt = false)
    {
        bool allFinished = true;
        foreach(AgentPKWBase a in agentList)
        {
            if(a.isRunning == true)
                allFinished = false;
        }

        if(allFinished)
        {
            if(isInterupt)
                m_AgentGroup.EndGroupEpisode();
            else
                m_AgentGroup.EndGroupEpisode();
            ResetScene();
        }
    }
    
    private void ResetScene()
    {
        Lesson previousLesson = currentLesson;
        currentLesson = curriculum[(int)envParameters.GetWithDefault("PKWParking_Parameters", startLessonDefault)];
        m_ResetTimer = 0;

        // neue umgebung laden bei bedarf
        bool isNewLesson = previousLesson != currentLesson; 
        if(isNewLesson)
        {
            maxTrainingSteps = currentLesson.maxSteps;           
            setEnvironment(currentLesson.environmentPrefabName);
            Debug.Log("New Lesson for Training Environment: "+this.name);
            foreach(AgentPKWBase agent in agentList)
                agent.setParkingLot(envController.parkingSpaceTransforms);

        }

        foreach(AgentPKWBase agent in agentList)
        {
            // m_AgentGroup.RegisterAgent(agent);
            agent.IsInGoal = false;
            agent.isRunning = true;
            agent.IsBreakAllowed    = currentLesson.agentControllBreak;
            agent.IsReverseAllowed  = currentLesson.agentControllReverse;
            agent.resetRaySensorMem();
            agentInformationList[agent].ResetInfo();
        }

        // Eingang verschieben
        if(currentLesson.randomiseEntranceAt != 0
        && episodeCounter % currentLesson.randomiseEntranceAt == 0 )
            envController.ZLineShuffle(1,3,0,3);
        
        // Geparkte Autos umstellen
        if(isNewLesson
        || currentLesson.randomiseCarsOnParkingLotAt != 0
        && episodeCounter % currentLesson.randomiseCarsOnParkingLotAt == 0)
            envController.SetAndShuffleCars(currentLesson.carsOnParkingLot);
        
        // Autos Spawnen
        if(currentLesson.randomiseSpawnAt == 0)
            throw new Exception("In der Curriculum Datei: RandomiseSpawnAt darf nicht 0 sein!");
        
        if(isNewLesson
        || currentLesson.randomiseSpawnAt != 0
        && episodeCounter % currentLesson.randomiseSpawnAt == 0)
            SpawnAgents(isRandom : true);

        // Trainingsareal rotieren
        if(currentLesson.randomiseEnvironmentRotationAt != 0
        && episodeCounter % currentLesson.randomiseEnvironmentRotationAt == 0)
             this.transform.rotation = GetRandomRot();

        // Wenn kein Random Spawn dann
        if(episodeCounter % currentLesson.randomiseSpawnAt != 0)
            SpawnAgents();

        envController.ClearOccupiedParkSpaces();
        episodeCounter++;
    }

    public void setEnvironment(string prefabName)
    {
        if(currentEnvironment != null)
        {
            Destroy(currentEnvironment);
            foreach(Transform car in carObstacleContainer)
            {
                Destroy(car.gameObject);
            }
        }
        GameObject envPrefab = null;
        foreach(GameObject environment in trainingEnvironmentList)
        {
            if(environment.name == prefabName)
                envPrefab = environment;
        }
        // für den fall eines Fehlers direkt abbrechen
        if(envPrefab == null)
            throw new Exception("Der name des Prefabs wurde unter den verfügbaren Prefabs"
                               +"nicht gefunden. Überprüfe die Curriculum datei und die "
                               +"Prefabs in der Prefab Liste des Training Controllers.");

        currentEnvironment = Instantiate(envPrefab, this.transform);
        envController = currentEnvironment.GetComponent<Parking_Lot_Environment_Controller>();
        envController.CarObstacleContainer = this.carObstacleContainer;
        spawnPoints = envController.spawnPoints;
    }

    // aufpassen wenn der agent noch keinen Spawn zugewiesen bekommen hat
    // könnte es zu problemen kommen
    private void SpawnAgents(bool isRandom = false)
    {
        // prüfe ob Spawn punkte valide sind
        foreach(Transform spawn in spawnPoints)
            if(spawn == null)
                throw new Exception("There is no spawn transform attached to the spawn point list.");

        if(agentList.Count > spawnPoints.Count)
        {
            Debug.LogError("Not enough Spawn-Points for the registered Agents!!");
        }

        Transform[] occupiedSpawnPoints = new Transform[spawnPoints.Count];
        Transform spawnPoint;
        
        int agentIndex = 0;
        foreach(AgentPKWBase agent in agentList)
        {
            // Bewegung zurücksetzen
            agent.RBody.velocity = Vector3.zero;
            agent.RBody.angularVelocity = Vector3.zero;

            if(isRandom)
            {
                // zufälligen, freien spawnpunkt finden
                do
                {
                    spawnPoint = spawnPoints[UnityEngine.Random.Range(0,spawnPoints.Count)];
                }while(isTransformInArray(spawnPoint, occupiedSpawnPoints));
                
                // wenn freien punkt gefunden, in die AgentInfo liste aufnehmen
                agentInformationList[agent].spawn = spawnPoint;

                // speichere besetzten spawn punkt und inkrementiere index
                occupiedSpawnPoints[agentIndex] = spawnPoint;
                agentIndex++;
            }
            // setze Agenten auf seinen Spawnpunkt und rotiere entsprechend
            agent.transform.position = agentInformationList[agent].spawn.position;
            if(agentInformationList[agent].spawn.parent.tag == "Respawn")
                agent.transform.localRotation = agentInformationList[agent].spawn.parent.localRotation;
            else
                agent.transform.localRotation = agentInformationList[agent].spawn.localRotation;
        }
    }

    //____Hilfsmethoden_______________________________________________

    private Quaternion GetRandomRot()
    {
        return Quaternion.Euler(0, UnityEngine.Random.Range(0.0f, 360.0f), 0);
    }

    private bool isTransformInArray(Transform transform, Transform[] array)
    {
        foreach(Transform arrTrans in array)
        {
            if(arrTrans == transform)
                return true;
        }
        return false;
    }

    public bool IsParkSpaceOccupied(Transform parkSpaceTransform)
    {
        return envController.IsParkSpaceOccupied(parkSpaceTransform);
    }
}

[System.Serializable]
public class Curriculum
{
    public Lesson[] Lessons;
}

[System.Serializable]
public class Lesson
{
    public string name;

    // environment
    public int maxSteps;
    public string environmentPrefabName;

    // agent controlls
    public bool agentControllBreak;
    public bool agentControllReverse;
    public bool rewardDriveForward = false;
    
    // amount of occupied parking spaces
    public int carsOnParkingLot;

    // time to remain in parking space till reward comes 
    public float remainTimeInParkingSpace;

    // randomise at each _ Episode
    public int randomiseSpawnAt;
    public int randomiseEntranceAt;
    public int randomiseCarsOnParkingLotAt;
    public int randomiseEnvironmentRotationAt;
}

public class AgentInfo
{
    public Transform        spawn;
    public List<Transform>  parkingSpacesInContact;
    private float           timeInParkingSpace;

    public AgentInfo()
    {
        parkingSpacesInContact = new List<Transform>();
        spawn = null;
        timeInParkingSpace = 0;
    }

    public float TimeInParkingSpace
    {
        get{return timeInParkingSpace;}
        set{timeInParkingSpace = value;}
    }

    public float GetTimeinSeconds()
    {
        return timeInParkingSpace * Time.fixedDeltaTime;
    }

    public void ResetInfo()
    {
        parkingSpacesInContact = new List<Transform>();
        timeInParkingSpace = 0;
    }
}