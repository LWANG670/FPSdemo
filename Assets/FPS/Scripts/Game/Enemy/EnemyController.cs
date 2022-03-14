using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class EnemyController : MonoBehaviour
{
    [Tooltip("敌人旋转速度")]
    public float OrientationSpeed = 10f;
    [Tooltip("寻路旋转半径")]
    public float PathReachingRadius = 2f;
    [Tooltip("攻击间隔")]
    public float DelayAfterWeaponSwap = 0f;
    [Tooltip("攻击时间")]
    public float DelayShot = 5.0f;

    //行为事件
    public UnityAction onAttack;
    public UnityAction onDetectedTarget;
    public UnityAction onLostTarget;
    public UnityAction onDamaged;

    //寻敌模块
    public DetectionModule DetectionModule { get; private set; }
    ActorsManager m_ActorsManager;
    Actor m_Actor;
    Collider[] m_SelfColliders;
    public GameObject KnownDetectedTarget => DetectionModule.KnownDetectedTarget;
    public bool IsTargetInAttackRange => DetectionModule.IsTargetInAttackRange;
    public bool IsSeeingTarget => DetectionModule.IsSeeingTarget;
    //---------------------------------------

    public PatrolPath PatrolPath { get; set; }
    private int m_PathDestinationNodeIndex;
    
    public UnityEngine.AI.NavMeshAgent NavMeshAgent { get; private set; }

    float m_LastTimeShot = 0.0f;


    void Start()
    {
        m_ActorsManager= GetComponent<ActorsManager>();
        m_Actor = GetComponent<Actor>();
        m_SelfColliders = GetComponentsInChildren<Collider>();

        var detectionModules = GetComponentsInChildren<DetectionModule>();
        DetectionModule = detectionModules[0];

        DetectionModule.onDetectedTarget += OnDetectedTarget;
        DetectionModule.onLostTarget += OnLostTarget;

        NavMeshAgent = GetComponent<NavMeshAgent>();
        //SetPathDestinationToClosestNode();
        NavMeshAgent.SetDestination(PatrolPath.PathNodes[1].position);
    }

    // Update is called once per frame
    void Update()
    {
        DetectionModule.HandleTargetDetection(m_Actor, m_SelfColliders);
        //StartPathDestination();
        m_LastTimeShot += Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Bullet")
        {

        }
    }

    bool IsPathValid()
    {
        return PatrolPath && PatrolPath.PathNodes.Count > 0;
    }

    public void SetPathDestinationToClosestNode()
    {
        if (IsPathValid())
        {
            int closestPathNodeIndex = 0;
            for (int i = 0; i < PatrolPath.PathNodes.Count; i++)
            {
                float distanceToPathNode = PatrolPath.GetDistanceToNode(transform.position, i);
                if (distanceToPathNode < PatrolPath.GetDistanceToNode(transform.position, closestPathNodeIndex))
                {
                    closestPathNodeIndex = i;
                }
            }

            m_PathDestinationNodeIndex = closestPathNodeIndex;
        }
        else
        {
            m_PathDestinationNodeIndex = 0;
        }
    }
    public void StartPathDestination()
    {
        if (IsPathValid())
        {
            UpdatePathDestination();
            if (NavMeshAgent)
            {
                NavMeshAgent.SetDestination(GetDestinationOnPath());
            }
        }
    }
    public Vector3 GetDestinationOnPath()
    {
        if (IsPathValid())
        {
            return PatrolPath.GetPositionOfPathNode(m_PathDestinationNodeIndex);
        }
        else
        {
            return transform.position;
        }
    }

    public void UpdatePathDestination(bool inverseOrder = false)
    {
        if (IsPathValid())
        {
            // Check if reached the path destination
            if ((transform.position - GetDestinationOnPath()).magnitude <= PathReachingRadius)
            {
                // increment path destination index
                m_PathDestinationNodeIndex =
                    inverseOrder ? (m_PathDestinationNodeIndex - 1) : (m_PathDestinationNodeIndex + 1);
                if (m_PathDestinationNodeIndex < 0)
                {
                    m_PathDestinationNodeIndex += PatrolPath.PathNodes.Count;
                }

                if (m_PathDestinationNodeIndex >= PatrolPath.PathNodes.Count)
                {
                    m_PathDestinationNodeIndex -= PatrolPath.PathNodes.Count;
                }
            }
        }
    }

    public void SetNavDestination(Vector3 destination)
    {
        if (NavMeshAgent)
        {
            NavMeshAgent.SetDestination(destination);
        }
    }
    //改变物体朝向
    public void OrientTowards(Vector3 lookPosition)
    {
        Vector3 lookDirection = Vector3.ProjectOnPlane(lookPosition - transform.position, Vector3.up).normalized;
        if (lookDirection.sqrMagnitude != 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation =
                Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * OrientationSpeed);
        }
    }

    public bool TryAtack(Vector3 enemyPosition)
    {
        //攻击频率
        if (DelayAfterWeaponSwap >= Time.time)
            return false;
        if (onAttack != null)
        {
            onAttack.Invoke();
            if (m_LastTimeShot >= DelayShot)
            {
                //GameObject.FindObjectOfType<PlayerBehavior>().GetComponent<Health>().TakeDamage(20, this.gameObject);
                m_LastTimeShot = 0;
            }
        }
        return true;
    }

    void OnDetectedTarget()
    {
        onDetectedTarget.Invoke();
        //发现目标后的相关处理
    }

    void OnLostTarget()
    {
        onLostTarget.Invoke();
        //失去敌人后的相关处理
    }
}
