using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.MLAgents;
using Unity.VisualScripting;

public class Park_Training_Controller : MonoBehaviour
{
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


    // Start is called before the first frame update
    void Start()
    {
        // laden der Curricula
        string curriculumText = curriculumFile.text;
        curriculum = JsonUtility.FromJson<Curriculum>(curriculumText).Lessons;

        //initialisieren der Agenten
        foreach(AgentPKW agent in agentList)
        {
            agent.Critic = this;
            m_AgentGroup.RegisterAgent(agent);
        }

        ResetScene();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        
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
    }

    public void GoalExitParkingLot()
    {
        /*  Ziel besteht allein darin aus dem Parkplatz 
            auf die straße und aus dem gebiet zu fahren
            so schnell es geht und ohne kollisionen.
        */
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

        Transform[] spawnBuffer = new Transform[spawnPoints.Count];
        foreach(AgentPKW agent in agentList)
        {
            // Bewegung zurücksetzen
            agent.RBody.velocity = Vector3.zero;
            agent.RBody.angularVelocity = Vector3.zero;
            do
            {
                
            }while(false);
            //agent.transform.position = UnityEngine.Random.Range(0, spawnPoints.Count)
        }
    }

    private bool isTransformInArray(Transform[] array, Transform transform)
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

}
