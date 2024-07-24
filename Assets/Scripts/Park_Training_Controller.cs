using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using System;
using System.Linq;

public class Park_Training_Controller : MonoBehaviour
{
    //Testvariablen
    // private int parkedCarCount = 10;
    // private float randomiseCounter = 0;
    // private float randomiseAt = 3;

    private Parking_Lot_Environment_Controller envController;

    [Header("Training")]
    public int MaxTrainingSteps = 10000;
    private int m_ResetTimer;
    private int episodeCounter = 0;
    
    public TextAsset curriculumFile;
    private Lesson[] curriculum;
    private Lesson   currentLesson;

    //[Header("Environment Parameter")]
    private EnvironmentParameters envParameters;


    [Header("Agent List and Parameters")]
    public List<AgentPKW> agentList;
    private SimpleMultiAgentGroup m_AgentGroup;

    private List<Transform> spawnPoints;

    private Dictionary<AgentPKW, AgentInfo> agentInformationList;


    // Start is called before the first frame update
    void Start()
    {
        // laden der Curricula
        envParameters         = Academy.Instance.EnvironmentParameters;
        string curriculumText = curriculumFile.text;
        curriculum            = JsonUtility.FromJson<Curriculum>(curriculumText).Lessons;

        // einen Handle auf den Umgebungscontroller holen
        int i = 0;
        Transform child;
        do
        {
            child = this.transform.GetChild(i);
            i++;
        }while(child.tag != "Environment");
        envController = child.GetComponent<Parking_Lot_Environment_Controller>();
        
        // instanziieren der Listen
        agentInformationList  = new Dictionary<AgentPKW, AgentInfo>();
        m_AgentGroup          = new SimpleMultiAgentGroup();
        spawnPoints           = envController.spawnPoints;

        //initialisieren der Agenten
        foreach(AgentPKW agent in agentList)
        {
            agent.Critic = this;
            agentInformationList.Add(agent, new AgentInfo());
            m_AgentGroup.RegisterAgent(agent);
        }

        ResetScene();
    }

    void FixedUpdate()
    {
        m_ResetTimer += 1;
        foreach(Agent a in m_AgentGroup.GetRegisteredAgents())
            a.AddReward(-1f/MaxTrainingSteps);
        if (m_ResetTimer >= MaxTrainingSteps && MaxTrainingSteps > 0)
        {
            FinishEpisode(true);
            Debug.Log("Folgendes Training hat die erlaubte Anzahl Steps überschritten: "+this.gameObject.name);
        }

        //zum testen, wenn agent auf parkplatz, dann belohnung berechnen
        // foreach(KeyValuePair<AgentPKW, AgentInfo> agentInfo in agentInformationList)
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
        foreach(KeyValuePair<AgentPKW, AgentInfo> agentInfoPair in agentInformationList)
        {
            GoalStayInParkingSpace(agentInfoPair);
        }
    }

    /*#########################################################*/
    /*                     Reward Methoden                     */
    /*#########################################################*/
    public void ExitTrainingArea(AgentPKW agent)
    {
        agent.AddReward(-1f);
        FinishEpisode();
    }

    public void ExitRoad(AgentPKW agent)
    {
        agent.AddReward(-0.2f);
    }

    public void CollisionWithAgent(AgentPKW agent)
    {
        agent.AddReward(-0.5f);
    }

    public void CollisionWithObstacle(AgentPKW agent)
    {
        agent.AddReward(-0.8f);
        if(currentLesson.agentControllReverse == false)
            FinishEpisode();
    }

    public void GoalEnterParkingSpace(AgentPKW agent, Transform parkingSpace)
    {
        /*  Das ganze soll wie folgt verlaufen
                - Agent meldet, welche Parklücke er betreten hat
                - Agent wird abgeschaltet und erhält punkte nachdem er nicht mehr rollt
                - Die Punktzahl/Belohnung hängt davon ab, wie weit er
                    vom ziel entfernt ist
                - Curriculum könnte beinhalten, dass der agent selbst abschaltet.
        */
        agentInformationList[agent].parkingSpacesInContact.Add(parkingSpace);
        agentInformationList[agent].TimeInParkingSpace = 0;
    }

    private void GoalStayInParkingSpace(KeyValuePair<AgentPKW, AgentInfo> agentInfoPair)
    {
        // nur wenn fahrzeug eine parklücke berührt
        if(agentInfoPair.Value.parkingSpacesInContact.Any())
        {
            // wenn lange genug in parklücke gewesen,
            // --> reward austeilen, agent abschalten
            if(agentInfoPair.Value.GetTimeinSeconds() >= this.currentLesson.remainTimeInParkingSpace)
            {
                agentInfoPair.Key.isRunning = false;
                agentInfoPair.Key.AddReward(CalcDistanceReward(agentInfoPair.Key.PKWBody, agentInfoPair.Value.parkingSpacesInContact.Last(), 0.5f));
                agentInfoPair.Key.AddReward(CalcRotationReward(agentInfoPair.Key.PKWBody, agentInfoPair.Value.parkingSpacesInContact.Last(), 0.5f));
                FinishEpisode();
            }
            // sonst --> reward austeilen
            else
                agentInfoPair.Value.TimeInParkingSpace += 1;
        }
    }

    public void ExitParkingSpace(AgentPKW agent, Transform parkingSpace)
    {
        agentInformationList[agent].parkingSpacesInContact.Remove(parkingSpace);
        agentInformationList[agent].TimeInParkingSpace = 0;
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
        return reward;
    }

    private float CalcRotationReward(Transform agentBody, Transform goal, float baseReward = 0)
    {
        float rotAgent  = agentBody.eulerAngles.y;
        float rotGoal   = goal.eulerAngles.y;
        float rotOffset = Mathf.Abs(rotGoal-rotAgent);

        // normalisiert auf einen bereich von -1 bis 1
        // --> vorwärts wird genauso gewertet wie rückwärts
        float offsetNorm = (2 * rotOffset / 180) - 1;

        // Rewardbestimmung entspricht der Distanz,
        // unterschied ist, dass die beiden Randwerte (0, 180)
        // die höchste belohnung geben sollen
        float reward = Mathf.Pow(offsetNorm, 2) * baseReward;
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
        if(isInterupt)
            m_AgentGroup.GroupEpisodeInterrupted();
        else
            m_AgentGroup.EndGroupEpisode();
        ResetScene();
    }
    
    private void ResetScene()
    {
        currentLesson = curriculum[(int)envParameters.GetWithDefault("", 0)];
        Debug.Log(currentLesson.name);

        m_ResetTimer = 0;
        foreach(AgentPKW agent in agentList)
        {
            // m_AgentGroup.RegisterAgent(agent);
            agent.isRunning = true;
            agent.IsBreakAllowed    = currentLesson.agentControllBreak;
            agent.IsReverseAllowed  = currentLesson.agentControllReverse;
            agentInformationList[agent].ResetInfo();
        }

        // Eingang verschieben
        if(currentLesson.randomiseEntranceAt != 0
        && episodeCounter % currentLesson.randomiseEntranceAt == 0 )
            envController.ZLineShuffle(1,3,0,3);
        
        // Geparkte Autos umstellen
        if(currentLesson.randomiseCarsOnParkingLotAt != 0
        && episodeCounter % currentLesson.randomiseCarsOnParkingLotAt == 0)
            envController.SetAndShuffleCars(currentLesson.carsOnParkingLot);
        
        // Autos Spawnen
        if(currentLesson.randomiseSpawnAt != 0
        && episodeCounter % currentLesson.randomiseSpawnAt == 0)
            SpawnAgents(isRandom : true);
        else
            SpawnAgents();

        // Trainingsareal rotieren
        if(currentLesson.randomiseEnvironmentRotationAt != 0
        && episodeCounter % currentLesson.randomiseEnvironmentRotationAt == 1)
             this.transform.rotation = GetRandomRot();

        episodeCounter++;
    }

    // aufpassen wenn der agent noch keinen Spawn zugewiesen bekommen hat
    // könnte es zu problemen kommen
    private void SpawnAgents(bool isRandom = false)
    {
        if(agentList.Count > spawnPoints.Count)
        {
            Debug.LogError("Not enough Spawn-Points for the registered Agents!!");
        }

        Transform[] occupiedSpawnPoints = new Transform[spawnPoints.Count];
        Transform spawnPoint;
        
        int agentIndex = 0;
        foreach(AgentPKW agent in agentList)
        {
            // Bewegung zurücksetzen
            agent.RBody.velocity = Vector3.zero;
            agent.RBody.angularVelocity = Vector3.zero;

            if(isRandom)
            {
                // zufälligen, freien spawnpunkt finden
                do
                {
                    spawnPoint = spawnPoints[UnityEngine.Random.Range(0,spawnPoints.Count-1)];
                }while(isTransformInArray(spawnPoint, occupiedSpawnPoints));
                
                // wenn freien punkt gefunden, in die AgentInfo liste aufnehmen
                agentInformationList[agent].spawn = spawnPoint;

                // speichere besetzten spawn punkt und inkrementiere index
                occupiedSpawnPoints[agentIndex] = spawnPoint;
                agentIndex++;
            }
            
            // setze Agenten auf seinen Spawnpunkt und rotiere entsprechend
            agent.transform.position = agentInformationList[agent].spawn.position;
            agent.transform.rotation = agentInformationList[agent].spawn.rotation;
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

    // agent controlls
    public bool agentControllBreak;
    public bool agentControllReverse;
    
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
