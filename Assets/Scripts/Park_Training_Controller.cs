using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.MLAgents;
using Unity.VisualScripting;
using System;

public class Park_Training_Controller : MonoBehaviour
{
    //Testvariablen
    private int parkedCarCount = 10;
    private float randomiseCounter = 0;
    private float randomiseAt = 3;



    [Header("Training")]
    public int MaxTrainingSteps = 10000;
    private int m_ResetTimer;
    private int espisodeCounter = 0;
    
    public TextAsset curriculumFile;
    private Lesson[] curriculum;
    private Lesson   currentLesson;

    [Header("Environment Parameter")]
    private EnvironmentParameters envParameters;


    [Header("Agent List and Parameters")]
    public List<AgentPKW> agentList;
    private SimpleMultiAgentGroup m_AgentGroup;

    public List<Transform> spawnPoints;

    private Dictionary<AgentPKW, Transform> agentInGoalCheckList;


    // Start is called before the first frame update
    void Start()
    {
        // laden der Curricula
        // string curriculumText = curriculumFile.text;
        // curriculum = JsonUtility.FromJson<Curriculum>(curriculumText).Lessons;

        agentInGoalCheckList = new Dictionary<AgentPKW, Transform>();
        m_AgentGroup = new SimpleMultiAgentGroup();
        //initialisieren der Agenten
        // int index = 0;
        foreach(AgentPKW agent in agentList)
        {
            agent.Critic = this;
            agentInGoalCheckList.Add(agent, null);
            // agent.indexName = "agent_"+index.ToString();
            m_AgentGroup.RegisterAgent(agent);
            // index++;
        }

        SpawnAgents();
        //ResetScene();
    }

    void FixedUpdate()
    {
        //zum testen, wenn agent auf parkplatz, dann belohnung berechnen
        foreach(KeyValuePair<AgentPKW, Transform> agentCheck in agentInGoalCheckList)
        {
            if(agentCheck.Value != null)
            {
                var distanceReward = CalcDistanceReward(agentCheck.Key.PKWBody, agentCheck.Value.transform, 1);
                var rotationReward = CalcRotationReward(agentCheck.Key.PKWBody, agentCheck.Value.transform, 1);
                Debug.Log(agentCheck.Key.gameObject.name
                          +" hat folgende Belohnung:\nDistanz: "
                          + distanceReward.ToString() + ", Rotation: " 
                          + rotationReward.ToString());
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // randomiseCounter += Time.deltaTime;
        // if(randomiseCounter > randomiseAt)
        // {
        //     SpawnAgents();
        //     randomiseCounter = 0f;
        // } 
    }

    /*#########################################################*/
    /*                     Reward Methoden                     */
    /*#########################################################*/
    public void ExitTrainingArea(AgentPKW agent)
    {

    }

    public void ExitRoad(AgentPKW agent)
    {

    }

    public void CollisionWithAgent(AgentPKW agent)
    {

    }

    public void CollisionWithObstacle(AgentPKW agent)
    {

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
        agentInGoalCheckList[agent] = parkingSpace;
    }

    public void GoalExitParkingSpace(AgentPKW agent, Transform parkingSpace)
    {
        agentInGoalCheckList[agent] = null;
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
    private void ResetScene()
    {
        currentLesson = curriculum[(int)envParameters.GetWithDefault("", 4)];
    }

    private void SpawnAgents()
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

            // zufälligen, freien spawnpunkt finden
            do
            {
                spawnPoint = spawnPoints[UnityEngine.Random.Range(0,spawnPoints.Count-1)];
            }while(isTransformInArray(spawnPoint, occupiedSpawnPoints));
            
            // wenn freien Punkt gefunden, setze agenten dahin
            agent.transform.position = spawnPoint.position;
            agent.transform.rotation = spawnPoint.rotation;

            // speichere besetzten spawn punkt und inkrementiere index
            occupiedSpawnPoints[agentIndex] = spawnPoint;
            agentIndex++;
        }
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
    
    // amount of occupied parking spaces
    public int carsOnParkingLot;
    
    // randomise at each _ Episode
    public int randomiseSpawnAt;
    public int randomiseEntranceAt;
    public int randomiseCarsOnParkingLotAt;
    public int randomiseEnvironmentRotationAt;
}
