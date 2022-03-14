using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyController))]
public class EnemyMobile : MonoBehaviour
{
    public enum AIState
    {
        Patrol,
        Follow,
        Attack,
    }

    public Animator Animator;

    [Tooltip("������ʱ��������Ч")]
    public ParticleSystem[] RandomHitSparks;
    [Tooltip("���ֵ���ʱ��������Ч")]
    public ParticleSystem[] OnDetectVfx;

    [Header("Sound")]
    public AudioClip MovementSound;
    public AudioClip OnDetectSfx;

    public AIState AiState { get; private set; }
    EnemyController m_EnemyController;
    AudioSource m_AudioSource;

    const string k_AnimAttackParameter1 = "Attack1";
    const string k_AnimAttackParameter2 = "Attack2";
    const string k_AnimOnDamagedParameter = "OnDamaged";
    // Start is called before the first frame update
    void Start()
    {
        m_EnemyController= GetComponent<EnemyController>();

        m_EnemyController.onAttack += OnAttack;
        m_EnemyController.onDetectedTarget += OnDetectedTarget;
        m_EnemyController.onLostTarget += OnLostTarget;
        //���ڳ�ʼλ�ã���ʼѲ��
        m_EnemyController.SetPathDestinationToClosestNode();
        m_EnemyController.onDamaged += OnDamaged;
        //��ʼ״̬Ѳ��
        AiState = AIState.Patrol;
        //�����Ƶ����Ȩ
        m_AudioSource = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateAiStateTransitions();
        UpdateCurrentAiState();
    }

    void UpdateAiStateTransitions()
    {
        switch (AiState)
        {
            case AIState.Follow:
                if (m_EnemyController.IsSeeingTarget && m_EnemyController.IsTargetInAttackRange)
                {
                    AiState = AIState.Attack;
                    m_EnemyController.SetNavDestination(transform.position);
                }
                break;
            case AIState.Attack:
                if (!m_EnemyController.IsTargetInAttackRange)
                {
                    AiState = AIState.Follow;
                }
                break;
        }
    }

    //�ж�
    void UpdateCurrentAiState()
    {
        switch (AiState)
        {
            case AIState.Patrol:
                m_EnemyController.UpdatePathDestination();
                m_EnemyController.SetNavDestination(m_EnemyController.GetDestinationOnPath());
                break;
            case AIState.Follow:
                if (!m_EnemyController.KnownDetectedTarget)
                    AiState = AIState.Patrol;
                m_EnemyController.SetNavDestination(m_EnemyController.KnownDetectedTarget.transform.position);
                m_EnemyController.OrientTowards(m_EnemyController.KnownDetectedTarget.transform.position);
                break;
            case AIState.Attack:
                if (Vector3.Distance(m_EnemyController.KnownDetectedTarget.transform.position,
                            m_EnemyController.DetectionModule.DetectionSourcePoint.position)
                        >= (m_EnemyController.DetectionModule.AttackRange))
                {
                    m_EnemyController.SetNavDestination(m_EnemyController.KnownDetectedTarget.transform.position);
                }
                else
                {
                    m_EnemyController.SetNavDestination(transform.position);
                }
                m_EnemyController.OrientTowards(m_EnemyController.KnownDetectedTarget.transform.position);
                m_EnemyController.TryAtack(m_EnemyController.KnownDetectedTarget.transform.position);
                break;
        }
    }

    void OnDetectedTarget()
    {
        if (AiState == AIState.Patrol)
        {
            //�л�ΪFollow����
            AiState = AIState.Follow;
        }

        for (int i = 0; i < OnDetectVfx.Length; i++)
        {
            OnDetectVfx[i].Play();
        }
    }

    void OnAttack()
    {
        if (Animator)
            Animator.SetTrigger(k_AnimAttackParameter1);
    }

    void OnDamaged()
    {
        if (RandomHitSparks.Length > 0)
        {
            int n = Random.Range(0, RandomHitSparks.Length - 1);
            if(n>=0)
                RandomHitSparks[n].Play();
        }
        if (Animator)
            Animator.SetTrigger(k_AnimOnDamagedParameter);
    }

    void OnLostTarget()
    {
        
    }
}
