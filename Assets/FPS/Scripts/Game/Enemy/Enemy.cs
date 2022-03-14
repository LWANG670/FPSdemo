//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.AI;
//using UnityEngine.Events;

//public class Enemy : MonoBehaviour
//{
//    // Start is called before the first frame update
//    //行为事件
//    public UnityAction onAttack;
//    public UnityAction onDetectedTarget;
//    public UnityAction onLostTarget;
//    public UnityAction onDamaged;

//    //寻敌模块
//    public DetectionModule DetectionModule { get; private set; }
//    ActorsManager m_ActorsManager;
//    Actor m_Actor;
//    Collider[] m_SelfColliders;


//    //巡逻模块
//    public PatrolPath PatrolPath { get; set; }
//    private int m_PathDestinationNodeIndex;//当前节点
//    public float PathReachingRadius = 2f;
//    public NavMeshAgent NavMeshAgent { get; private set; }
//    void Start()
//    {
//        m_Actor= GetComponent<Actor>();
//        m_SelfColliders = GetComponentsInChildren<Collider>();

//        var detectionModules = GetComponentsInChildren<DetectionModule>();
//        DetectionModule = detectionModules[0];

//        DetectionModule.onDetectedTarget += OnDetectedTarget;
//        DetectionModule.onLostTarget += OnLostTarget;

//        NavMeshAgent = GetComponent<NavMeshAgent>();
//        SetPathDestinationToClosestNode();
//        NavMeshAgent.SetDestination(PatrolPath.PathNodes[1].position);
//    }

//    // Update is called once per frame
//    void Update()
//    {
//        DetectionModule.HandleTargetDetection(m_Actor, m_SelfColliders);
//        StartPathDestination();
//    }

//    private void OnTriggerEnter(Collider other)
//    {
//        if (other.tag == "Bullet")
//        {
 
//        }   
//    }

//    bool IsPathValid()
//    {
//        return PatrolPath && PatrolPath.PathNodes.Count > 0;
//    }

//    public void SetPathDestinationToClosestNode()
//    {
//        if (IsPathValid())
//        {
//            int closestPathNodeIndex = 0;
//            for (int i = 0; i < PatrolPath.PathNodes.Count; i++)
//            {
//                float distanceToPathNode = PatrolPath.GetDistanceToNode(transform.position, i);
//                if (distanceToPathNode < PatrolPath.GetDistanceToNode(transform.position, closestPathNodeIndex))
//                {
//                    closestPathNodeIndex = i;
//                }
//            }

//            m_PathDestinationNodeIndex = closestPathNodeIndex;
//        }
//        else
//        {
//            m_PathDestinationNodeIndex = 0;
//        }
//    }

//    public Vector3 GetDestinationOnPath()
//    {
//        if (IsPathValid())
//        {
//            return PatrolPath.GetPositionOfPathNode(m_PathDestinationNodeIndex);
//        }
//        else
//        {
//            return transform.position;
//        }
//    }

//    public void StartPathDestination()
//    {
//        if (IsPathValid())
//        {
//            UpdatePathDestination();
//            if (NavMeshAgent)
//            {
//                NavMeshAgent.SetDestination(GetDestinationOnPath());
//            }
//        }
//    }
//    public void UpdatePathDestination(bool inverseOrder = false)
//    {
//        if (IsPathValid())
//        {
//            // Check if reached the path destination
//            if ((transform.position - GetDestinationOnPath()).magnitude <= PathReachingRadius)
//            {
//                // increment path destination index
//                m_PathDestinationNodeIndex =
//                    inverseOrder ? (m_PathDestinationNodeIndex - 1) : (m_PathDestinationNodeIndex + 1);
//                if (m_PathDestinationNodeIndex < 0)
//                {
//                    m_PathDestinationNodeIndex += PatrolPath.PathNodes.Count;
//                }

//                if (m_PathDestinationNodeIndex >= PatrolPath.PathNodes.Count)
//                {
//                    m_PathDestinationNodeIndex -= PatrolPath.PathNodes.Count;
//                }
//            }
//        }
//    }

//    void OnDetectedTarget()
//    {
//        onDetectedTarget.Invoke();
//        //发现目标后的相关处理
//    }

//    void OnLostTarget()
//    {
//        onLostTarget.Invoke();
//        //失去敌人后的相关处理
//    }
//}
