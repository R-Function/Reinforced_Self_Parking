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
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        
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
