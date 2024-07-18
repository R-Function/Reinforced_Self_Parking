using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Unity.MLAgents;

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

    private void ResetScene()
    {

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
