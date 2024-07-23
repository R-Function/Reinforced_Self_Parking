// using Unity.MLAgents;
// using UnityEngine;
// using System.Collections.Generic;
// using System.IO;

// public class OrderedCrossingController : MonoBehaviour
// {    
//     public bool automaticAreaPlacement;
//     [Range(35,50)]
//     public int areaSpacing;

//     [Header("Environment Randomisation")]
//     public bool randomRotationOfAgent = true;
//     public bool useSpawn = false;
//     public float spawnMarginAgent = 0.95f;
//     [Range(1f,10f)]
//     public float minSpawnDistanceToOthers = 1f;

//     [Header("Max Environment Steps")] 
//     public int MaxEnvironmentSteps = 25000;

//     [Header("Environment Data")]
//     public Transform goalContainer;
//     public List<Transform> goals;
//     public Transform bridge;
//     public Transform spawn;
//     private Bounds startSideBounds;
//     public GameObject startSide;
//     private Bounds goalSideBounds;
//     public GameObject goalSide;
//     private Bounds riverBounds;
//     public GameObject riverRegion;

//     public List<OrderedCrossingAgent> agents;
//     [Range(0.01f,1f)]

//     private Lesson[] curriculum;
//     private Lesson currentLesson;

//     private int m_ResetTimer;
//     private int episodeCounter = 0;
//     private SimpleMultiAgentGroup m_AgentGroup;
//     private EnvironmentParameters envParameters;


//     void Start()
//     {
//         MaxEnvironmentSteps = 8000;

//         if(automaticAreaPlacement)
//             PlaceOnGrid();

//         startSideBounds = startSide.GetComponent<Collider>().bounds;
//         goalSideBounds  = goalSide.GetComponent<Collider>().bounds;
//         riverBounds     = riverRegion.GetComponent<Collider>().bounds;
//         envParameters   = Academy.Instance.EnvironmentParameters;
//         m_AgentGroup    = new SimpleMultiAgentGroup();

//         //abändern der GoalSideBounds
//         ResizeGoalSideBounds(20);

//         // registrieren der Agenten
//         foreach (OrderedCrossingAgent a in agents)
//         {
//             a.Bridge        = bridge;
//             a.Goals         = goals;
//             a.EnvController = this;
//             m_AgentGroup.RegisterAgent(a);
//         }

//         // laden der Curricula
//         string curriculumText = File.ReadAllText(@"/home/attac1/Dokumente/Machine_Learning_Projekt/ml-agents/config/test_lauf/curriculum.json");
//         curriculum = JsonUtility.FromJson<Curriculum>(curriculumText).Lessons;

//         ResetScene();
//     }

//     // Update is called once per frame
//     void FixedUpdate()
//     {
//         m_ResetTimer += 1;
//         foreach(Agent a in m_AgentGroup.GetRegisteredAgents())
//             a.AddReward(-0.5f/MaxEnvironmentSteps);
//         if (m_ResetTimer >= MaxEnvironmentSteps && MaxEnvironmentSteps > 0)
//         {
//             FinishEpisode(true);
//             Debug.Log("Folgendes Training hat die erlaubte Anzahl Steps überschritten: "+this.gameObject.name);
//         }
//     }

//     public void ExitMap(OrderedCrossingAgent agent)
//     {
//         agent.AddReward(-1f);

//         if(IsFinished())
//             FinishEpisode();
//         else
//             agent.gameObject.SetActive(false);
//         // Debug.Log("belohnung von "+agent.gameObject.name+" ist gleich: "+agent.M_Info.groupReward.ToString());
//         // Debug.Log("Failed Agents: "+m_NumberOfRetiredAgents.ToString());
//     }

//     public void EnterFailState(OrderedCrossingAgent agent)
//     {
//         agent.AddReward(-0.7f);

//         if(IsFinished())
//             FinishEpisode();
//         else
//             agent.gameObject.SetActive(false);
//     }

//     public void CollisionWithCoAgent(OrderedCrossingAgent agent)
//     {
//         agent.AddReward(-0.1f);
//     }

//     public void EnteredGoal(OrderedCrossingAgent agent)
//     {
//         agent.AddReward(1f);
//         if(IsFinished())
//             FinishEpisode();
//     }

//     public void ExitGoal(OrderedCrossingAgent agent)
//     {
//         agent.AddReward(-0.2f);
//     }


//     private void ResetScene()
//     {
//         // die derzeitige Lektion laden
//         currentLesson = curriculum[(int)envParameters.GetWithDefault("bridge_crossing_parameters", 4)];
        
//         if(episodeCounter % (int)currentLesson.CurEnvRandomisation == 0)
//             Debug.Log(this.gameObject.name+" erreicht episode: "+episodeCounter.ToString()+"\n initialisiere neue Umgebung.");
//         //reset counter
//         m_ResetTimer = 0;
//         if(episodeCounter % currentLesson.CurEnvRandomisation == 0)
//             NewEnvironment();

//         foreach(OrderedCrossingAgent a in agents)
//         {
//             // Bewegung zurücksetzen
//             a.RBody.velocity = Vector3.zero;
//             a.RBody.angularVelocity = Vector3.zero;
            
//             // zufällige rotation für die Agenten setzen
//             a.transform.rotation = GetRandomRot();

//             // lektion spezifische eigenschaften setzen
//             a.maxAngVel = currentLesson.AgentMaxAngVel;
//             a.PhysicsControll = currentLesson.AgentPhysControll;

//             //reaktivieren
//             a.gameObject.SetActive(true);
//             m_AgentGroup.RegisterAgent(a);
//             a.InGoal    = false;
//             a.IsRunning = true;
//         }
//         AgentsRandomSpawn();

//         episodeCounter++;
//     }

//     private void NewEnvironment()
//     {
//         this.transform.rotation = Quaternion.Euler(0, 0, 0);
        
//         // zufällige position des Ziels auf der anderen Seite des Ufers, zwischenrechnung dient der anpassung der bounds
//         goalContainer.position = GetRandomPos(goalSideBounds, (1f-goalContainer.gameObject.GetComponent<Collider>().bounds.extents.x / goalSideBounds.extents.x),(1f-goalContainer.gameObject.GetComponent<Collider>().bounds.extents.z / goalSideBounds.extents.z), y:goalContainer.position.y);
//         // zufällige Position und Curriculum bestimmte breite der Brücke
//         bridge.localScale = new Vector3(currentLesson.getBridgeWidth(),bridge.localScale.y,bridge.localScale.z);
//         bridge.position = GetRandomPos(riverBounds, (0.95f-0.5f*bridge.localScale.x / riverBounds.extents.x), 0, y:bridge.position.y);
        
//         //Debug.Log("Folgendes Training hat die erlaubte Anzahl Steps überschritten: "+this.gameObject.name);
//         //Debug.Log("Für Training "+this.gameObject.name+" sind die bounds der Bruecke: "+riverBounds.ToString());
//         spawn.localScale = new Vector3(currentLesson.CurSpawnSize, 0.1f, currentLesson.CurSpawnSize);
//         spawn.position = GetRandomPos(startSideBounds,(0.95f-0.5f*spawn.localScale.x / startSideBounds.extents.x),(0.95f-spawn.gameObject.GetComponent<Collider>().bounds.extents.z / startSideBounds.extents.z), y:spawn.position.y);
    
//         this.transform.rotation = GetRandomRot();
//     }

//     private Quaternion GetRandomRot()
//     {
//         return Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
//     }

//     private Vector3 GetRandomPos(Bounds bounds, float xMarginMult, float zMarginMult, float y = 1.1f)
//     {
//         var randomPosX = Random.Range(-bounds.extents.x * xMarginMult + bounds.center.x,
//                                        bounds.extents.x * xMarginMult + bounds.center.x);
//         var randomPosZ = Random.Range(-bounds.extents.z * zMarginMult + bounds.center.z,
//                                        bounds.extents.z * zMarginMult + bounds.center.z);
//         return new Vector3(randomPosX, y,randomPosZ);
//     }

//     private void AgentsRandomSpawn()
//     {
//         int posSearchCounter = 0;
//         // zufällige position für alle agenten
//         List<Vector3> forbiddenPositions = new List<Vector3>();

//         if(useSpawn)
//             forbiddenPositions.Add(getRandomPosInCircle(spawn.position, spawn.localScale.x/2));
//         else
//             forbiddenPositions.Add(GetRandomPos(startSideBounds, 
//                                                 spawnMarginAgent, 
//                                                 spawnMarginAgent));
//         agents[0].transform.position = forbiddenPositions[0];
        
//         bool badPosition = false;
//         for(int i = 1; i < agents.Count;)
//         {
//             posSearchCounter++;
//             if(posSearchCounter >= 100)
//                 throw new System.Exception("Problems while looking for a fitting spawn.");
            
//             badPosition = false;
//             Vector3 newPos;
//             if(useSpawn)
//                 newPos = getRandomPosInCircle(spawn.position, spawn.localScale.x/2);
//             else
//                 newPos = GetRandomPos(startSideBounds, 
//                                       spawnMarginAgent,
//                                       spawnMarginAgent);
//             foreach(Vector3 pos in forbiddenPositions)
//             {
//                 if(Vector3.Distance(newPos, pos) < minSpawnDistanceToOthers + getMaxExtents(agents[i]))
//                     badPosition = true;
//             }
//             if(!badPosition)
//             {
//                 forbiddenPositions.Add(newPos);
//                 agents[i].transform.position = newPos;
//                 i++;
//             }
//         }
//     }
//     private Vector3 getRandomPosInCircle(Vector3 center, float radius)
//     {
//         Vector2 spawnPoint2D = Random.insideUnitCircle * radius;
//         return  new Vector3(spawnPoint2D.x + center.x, center.y, spawnPoint2D.y + center.z);
//     }

//     // vergleicht die x ausdehnung mit der z ausdehnung und gibt größere zurück
//     private float getMaxExtents(OrderedCrossingAgent agent)
//     {
//         float max =  agent.gameObject.GetComponent<Collider>().bounds.extents.x*2;
//         if(max >= agent.gameObject.GetComponent<Collider>().bounds.extents.z*2)
//             return max;
//         else
//             return agent.gameObject.GetComponent<Collider>().bounds.extents.z*2;
//     }

//     private bool IsFinished()
//     {
//         int finishedAgentCount = 0;
//         foreach(var agent in this.agents)
//         {
//             if(!agent.IsRunning)
//                 finishedAgentCount++;
//         }
//         // Debug.Log("Anzahl fertiger agenten: "+finishedAgentCount.ToString());
         
//         return finishedAgentCount >= agents.Count;
//     }

//     private void FinishEpisode(bool isInterupt = false)
//     {
//         // float groupReward = 0;
//         // foreach(OrderedCrossingAgent a in this.m_AgentGroup.GetRegisteredAgents())
//         //     if(a.InGoal)
//         //         groupReward+=0.5f;
//         // if(groupReward > 0.5f)
//         //     m_AgentGroup.SetGroupReward(groupReward);

//         if(isInterupt)
//             m_AgentGroup.GroupEpisodeInterrupted();
//         else
//             m_AgentGroup.EndGroupEpisode();
//         ResetScene();
//     }

//     // diese methode soll den möglichen Bereich, in dem das
//     // ziel sich befinden kann begrenzen, damit das ziel zB
//     // nicht direkt nach einer Brücke kommen kann
//     private void ResizeGoalSideBounds(int cutoffSize)
//     {
//         // Vector3 cutoffVector = (goalSideBounds.center - startSideBounds.center).normalized * cutoffSize/2;
//         Vector3 newCenter = new Vector3(goalSideBounds.center.x, goalSideBounds.center.y, goalSideBounds.center.z + cutoffSize/2);
//         Vector3 newSize = new Vector3(goalSideBounds.size.x, goalSideBounds.size.y, goalSideBounds.size.z - cutoffSize);
//         goalSideBounds = new Bounds(newCenter, newSize);
//         Debug.Log(goalSideBounds);
//     }

//     //platziert trainingsareal an seiner stelle auf einem
//     //quadratischen gitter
//     // !!funktioniert leider noch nicht!!
//     private void PlaceOnGrid()
//     {
//         try
//         {
//             GameObject[] areas = GameObject.FindGameObjectsWithTag("TrainingArea");
//             int gridSide = CalcSmallestSquareSide(areas.Length);
//             string indexString = this.gameObject.name.Split(' ')[1];
//             int x = 0;
//             int z = 0;
//             int areaIndex = System.Int32.Parse(indexString.Substring(1, indexString.Length - 2));
//             for(;areaIndex >= gridSide; x++)
//             {
//                 areaIndex -= gridSide;
//                 if(areaIndex <= gridSide)
//                   z = areaIndex;
//             }
//             this.transform.localPosition= new Vector3(x*areaSpacing,0,z*areaSpacing);
//         }
//         catch (System.Exception e)
//         {
//             Debug.LogError(e.Message);
//         }
//     }

//     //Diese Funktion ist dazu die kleinst mögliche
//     //seitenlänge für ein quadratisches Gitter zu finden
//     //in das n elemente passen
//     private int CalcSmallestSquareSide(int n)
//     {
//         //berechnet die wurzel, schneidet die nachkommastelle
//         //ab und addiert 1 für das nächstgrößere quadrat
//         float side = (int)System.Math.Sqrt(n);
//         Debug.Log(side == System.Math.Truncate(side));
//         if(side == (float)((int)side))
//             return (int)side;
//         else
//             return (int)side+1;
//     }
// }

// // // [System.Serializable]
// // public class Curriculum
// // {
// //     public Lesson[] Lessons;
// // }

// // // [System.Serializable]
// // public class Lesson
// // {
// //     //Curriculum Name
// //     public string Name;

// //     //AgentSettings
// //     public float AgentMaxAngVel;
// //     public bool AgentPhysControll;

// //     //Curriculum
// //     public int[] CurBridgeWidth;
// //     public int CurEnvRandomisation;
// //     public int CurSpawnSize;

// //     public float getBridgeWidth()
// //     {
// //         return Random.Range(CurBridgeWidth[0], CurBridgeWidth[1]);
// //     }
// // }

