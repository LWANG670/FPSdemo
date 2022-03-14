using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

public class DetectionModule : MonoBehaviour
{
    //用于搜寻目标对象
    [Tooltip("搜寻点位置")]
    public Transform DetectionSourcePoint;
    [Tooltip("可视距离")]
    public float DetectionRange = 10f;
    [Tooltip("可攻击距离")]
    public float AttackRange = 0.5f;
    [Tooltip("脱离可攻击判定时间")]
    public float KnownTargetTimeout = 4f;

    public UnityAction onDetectedTarget;//发现目标行为
    public UnityAction onLostTarget;//失去目标行为

    public GameObject KnownDetectedTarget { get; private set; }//已搜寻到的对象

    public bool IsTargetInAttackRange { get; private set; }//是否在可攻击距离内

    public bool IsSeeingTarget { get; private set; }//是否已看见目标

    public bool HadKnownTarget { get; private set; }

    ActorsManager m_ActorsManager;//目标管理集合

    protected float TimeLastSeenTarget = Mathf.NegativeInfinity;

    void Start()
    {
        m_ActorsManager = FindObjectOfType<ActorsManager>();//获得所有可访问对象
    }

    //用于检索
    public virtual void HandleTargetDetection(Actor actor, Collider[] selfColliders)
    {
        //失去目标对象
        if (KnownDetectedTarget && !IsSeeingTarget && (Time.time - TimeLastSeenTarget) > KnownTargetTimeout)
        {
            KnownDetectedTarget = null;
        }
        //计算最近距离
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
                    // Check for obstructions 使用射线
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
                            //获得射线的最近接触对象
                            closestValidHit = hit;
                            foundValidHit = true;
                        }
                    }

                    if (foundValidHit)
                    {
                        Actor hitActor = closestValidHit.collider.GetComponentInParent<Actor>();
                        //说明可达
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
        //有效对象且在可攻击范围内
        IsTargetInAttackRange = KnownDetectedTarget != null &&
                                Vector3.Distance(transform.position, KnownDetectedTarget.transform.position) <=
                                AttackRange;

        if (!HadKnownTarget && KnownDetectedTarget != null)
        {
            //触发寻敌
            OnDetect();
        }

        if (HadKnownTarget && KnownDetectedTarget == null)
        {
            //触发缺失
            OnLostTarget();
        }

        HadKnownTarget = KnownDetectedTarget != null;
    }

    public virtual void OnLostTarget() => onLostTarget?.Invoke();//可空对象

    public virtual void OnDetect() => onDetectedTarget?.Invoke();

    //当受到攻击时，转换目标对象
    public virtual void OnDamaged(GameObject damageSource)
    {
        TimeLastSeenTarget = Time.time;
        KnownDetectedTarget = damageSource;
    }
}
