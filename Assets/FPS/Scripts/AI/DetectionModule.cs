using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

public class DetectionModule : MonoBehaviour
{
    //������ѰĿ�����
    [Tooltip("��Ѱ��λ��")]
    public Transform DetectionSourcePoint;
    [Tooltip("���Ӿ���")]
    public float DetectionRange = 10f;
    [Tooltip("�ɹ�������")]
    public float AttackRange = 0.5f;
    [Tooltip("����ɹ����ж�ʱ��")]
    public float KnownTargetTimeout = 4f;

    public UnityAction onDetectedTarget;//����Ŀ����Ϊ
    public UnityAction onLostTarget;//ʧȥĿ����Ϊ

    public GameObject KnownDetectedTarget { get; private set; }//����Ѱ���Ķ���

    public bool IsTargetInAttackRange { get; private set; }//�Ƿ��ڿɹ���������

    public bool IsSeeingTarget { get; private set; }//�Ƿ��ѿ���Ŀ��

    public bool HadKnownTarget { get; private set; }

    ActorsManager m_ActorsManager;//Ŀ�������

    protected float TimeLastSeenTarget = Mathf.NegativeInfinity;

    void Start()
    {
        m_ActorsManager = FindObjectOfType<ActorsManager>();//������пɷ��ʶ���
    }

    //���ڼ���
    public virtual void HandleTargetDetection(Actor actor, Collider[] selfColliders)
    {
        //ʧȥĿ�����
        if (KnownDetectedTarget && !IsSeeingTarget && (Time.time - TimeLastSeenTarget) > KnownTargetTimeout)
        {
            KnownDetectedTarget = null;
        }
        //�����������
        float sqrDetectionRange = DetectionRange * DetectionRange;
        IsSeeingTarget = false;
        float closestSqrDistance = Mathf.Infinity;
        foreach (Actor otherActor in m_ActorsManager.Actors)
        {
            if (otherActor.Affiliation != actor.Affiliation)
            {
                float sqrDistance = (otherActor.transform.position - DetectionSourcePoint.position).sqrMagnitude;
                if (sqrDistance < sqrDetectionRange && sqrDistance < closestSqrDistance)
                {
                    // Check for obstructions ʹ������
                    RaycastHit[] hits = Physics.RaycastAll(DetectionSourcePoint.position,
                        (otherActor.AimPoint.position - DetectionSourcePoint.position).normalized, DetectionRange,
                        -1, QueryTriggerInteraction.Ignore);
                    RaycastHit closestValidHit = new RaycastHit();
                    closestValidHit.distance = Mathf.Infinity;
                    bool foundValidHit = false;
                    foreach (var hit in hits)
                    {
                        if (!selfColliders.Contains(hit.collider) && hit.distance < closestValidHit.distance)
                        {
                            //������ߵ�����Ӵ�����
                            closestValidHit = hit;
                            foundValidHit = true;
                        }
                    }

                    if (foundValidHit)
                    {
                        Actor hitActor = closestValidHit.collider.GetComponentInParent<Actor>();
                        //˵���ɴ�
                        if (hitActor == otherActor)
                        {
                            IsSeeingTarget = true;
                            closestSqrDistance = sqrDistance;

                            TimeLastSeenTarget = Time.time;
                            KnownDetectedTarget = otherActor.AimPoint.gameObject;
                        }
                    }
                }
            }
        }
        //��Ч�������ڿɹ�����Χ��
        IsTargetInAttackRange = KnownDetectedTarget != null &&
                                Vector3.Distance(transform.position, KnownDetectedTarget.transform.position) <=
                                AttackRange;

        if (!HadKnownTarget && KnownDetectedTarget != null)
        {
            //����Ѱ��
            OnDetect();
        }

        if (HadKnownTarget && KnownDetectedTarget == null)
        {
            //����ȱʧ
            OnLostTarget();
        }

        HadKnownTarget = KnownDetectedTarget != null;
    }

    public virtual void OnLostTarget() => onLostTarget?.Invoke();//�ɿն���

    public virtual void OnDetect() => onDetectedTarget?.Invoke();

    //���ܵ�����ʱ��ת��Ŀ�����
    public virtual void OnDamaged(GameObject damageSource)
    {
        TimeLastSeenTarget = Time.time;
        KnownDetectedTarget = damageSource;
    }
}
