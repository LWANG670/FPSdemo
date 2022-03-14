using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Player;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    //子弹跟踪类型..直射/跟踪
    public enum BulletType
    {
        Standard,
        Seeker
    }
    //子弹攻击类型
    public enum DamageType
    {
        Direct,
        Explosion
    }
    public class ProjectileStandard : ProjectileBase
    {
        [Tooltip("子弹类型:直线或者跟踪")]
        public BulletType BulletType = BulletType.Standard;
        [Tooltip("子弹伤害类型:直线或者爆炸伤害")]
        public DamageType DamageType = DamageType.Direct;
        [Tooltip("子弹生存时间")]
        public float MaxLifeTime = 5f;

        [Header("粒子模块")]
        [Tooltip("碰撞到物体时的粒子效果")]
        public GameObject ImpactVfx;
        [Tooltip("粒子效果持续时间")]
        public float ImpactVfxLifetime = 5f;
        [Tooltip("命中偏移")]
        public float ImpactVfxSpawnOffset = 0.1f;

        [Header("Damage模块")]
        [Tooltip("可射中目标的Layer")]
        public LayerMask HittableLayers = -1;
        [Tooltip("子弹速度")]
        public float Speed = 20f;
        [Tooltip("子弹伤害")]
        public float Damage = 1f;
        [Tooltip("射中的音效")]
        public AudioClip ImpactSfxClip;
        

        [Header("寻敌效果")]
        [Tooltip("寻敌搜索时间间隔")]
        public float TargetListUpdateRate = 1.0f;

        [Header("精确碰撞")]
        [Tooltip("子弹半径")]
        public float Radius = 0.01f;
        [Tooltip("子弹开始的位置")]
        public Transform Root;
        [Tooltip("子弹头部的位置")]
        public Transform Tip;


        [Header("爆炸效果")]
        [Tooltip("爆炸力")]
        public float ExplosionForce = 5.0f;
        [Tooltip("爆炸半径")]
        public float ExplosionRadius = 10.0f;

        [Header("力效果")]
        [Tooltip("重力效果")]
        public float GravityDownAcceleration = 0f;



        ProjectileBase m_ProjectileBase;//保存当前子弹的自身属性（发射对象等）
        Vector3 m_Velocity;//发射后的移动方向
        Vector3 m_LastRootPosition;
        GameObject TargetObject;//寻敌目标
        List<Collider> m_IgnoredColliders;//被忽视的对象
        AudioSource m_AudioSource;

        //OnEnable区别于Awake,要求其活动时才开始执行，都先于Start
        void OnEnable()
        {
            m_ProjectileBase = GetComponent<ProjectileBase>();
            m_ProjectileBase.OnShoot += OnShoot;
            m_AudioSource = GetComponent<AudioSource>();

            Destroy(gameObject, MaxLifeTime);
        }

        // Update is called once per frame
        void Update()
        {
            transform.position += m_Velocity * Time.deltaTime;
            //重力
            if (GravityDownAcceleration > 0)
            {
                // add gravity to the projectile velocity for ballistic effect
                m_Velocity += Vector3.down * GravityDownAcceleration * Time.deltaTime;
            }
            //这里对跟踪导弹进行控制
            {

            }
            //处理命中
            {
                //RatcastHit用于存储发射射线后产生的碰撞信息
                RaycastHit closestHit = new RaycastHit();
                closestHit.distance = Mathf.Infinity;
                bool foundHit = false;

                Vector3 displacementSinceLastFrame = Tip.position - m_LastRootPosition;//形成一个椭圆球体的碰撞块

                RaycastHit[] hits = Physics.SphereCastAll(m_LastRootPosition, Radius,
                    displacementSinceLastFrame.normalized, displacementSinceLastFrame.magnitude, HittableLayers,
                    QueryTriggerInteraction.Collide);
                //寻找最近的碰撞体
                foreach (var hit in hits)
                {
                    if (IsHitValid(hit) && hit.distance < closestHit.distance)
                    {
                        foundHit = true;
                        closestHit = hit;
                    }
                }
                if (foundHit)
                {
                    if (closestHit.distance <= 0f)
                    {
                        closestHit.point = Root.position;
                        closestHit.normal = -transform.forward;
                    }

                    OnHit(closestHit.point, closestHit.normal, closestHit.collider);
                }
            }
            m_LastRootPosition = Root.position;//更新上一次位置
        }

        //隐藏父类继承
        new void OnShoot()
        {
            //添加自身
            m_LastRootPosition = Root.position;
            m_Velocity = transform.forward * Speed;
            m_IgnoredColliders = new List<Collider>();
            Collider[] ownerColliders = m_ProjectileBase.Owner.GetComponentsInChildren<Collider>();
            m_IgnoredColliders.AddRange(ownerColliders);

            PlayerWeaponsManager playerWeaponsManager = m_ProjectileBase.Owner.GetComponent<PlayerWeaponsManager>();
            if (playerWeaponsManager)
            {
                Vector3 cameraToMuzzle = (m_ProjectileBase.InitialPosition -
                                          playerWeaponsManager.WeaponCamera.transform.position);
                if (Physics.Raycast(playerWeaponsManager.WeaponCamera.transform.position, cameraToMuzzle.normalized,
                    out RaycastHit hit, cameraToMuzzle.magnitude, HittableLayers, QueryTriggerInteraction.Collide))
                {
                    if (IsHitValid(hit))
                    {
                        OnHit(hit.point, hit.normal, hit.collider);
                    }
                }
            }
        }
        //检验当前碰撞有效性
        bool IsHitValid(RaycastHit hit)
        {
            if (hit.collider.GetComponent<IgnoreHitDetection>())
                return false;
            //不可收到伤害
            if (hit.collider.isTrigger && hit.collider.GetComponent<Damageable>() == null)
                return false;
            if (m_IgnoredColliders != null && m_IgnoredColliders.Contains(hit.collider))
                return false;
            return true;
        }

        //击中目标后
        void OnHit(Vector3 point, Vector3 normal, Collider collider)
        {
            if (DamageType == DamageType.Direct)
            {
                Damageable damageable = collider.GetComponent<Damageable>();
                if (damageable)
                {
                    damageable.InflictDamage(Damage, false, m_ProjectileBase.Owner);
                }

            }
            else if (DamageType == DamageType.Explosion)
            {
                //获得当前附近的所有物体
                Collider[] cols = Physics.OverlapSphere(transform.position, ExplosionRadius);
                //为附近角色施加力
                List<Rigidbody> rigidbodies = new List<Rigidbody>();
                foreach (Collider col in cols)
                {
                    //范围伤害减免
                    float damageAmount = Damage * (1 / Vector3.Distance(transform.position, col.transform.position));
                    Damageable damageable = collider.GetComponent<Damageable>();
                    if (damageable)
                    {
                        damageable.InflictDamage(Damage, false, m_ProjectileBase.Owner);
                    }
                    //产生爆炸力
                    if (col.attachedRigidbody != null)
                    {
                        var curForce = ExplosionForce * (1 / Vector3.Distance(transform.position, col.transform.position));
                        if(col.attachedRigidbody)
                            col.attachedRigidbody.AddExplosionForce(curForce, transform.position, ExplosionRadius, 1, ForceMode.Impulse);
                    }
                }
            }
            // impact vfx
            if (ImpactVfx)
            {
                GameObject impactVfxInstance = Instantiate(ImpactVfx, point + (normal * ImpactVfxSpawnOffset),
                    Quaternion.LookRotation(normal));
                if (ImpactVfxLifetime > 0)
                {
                    Destroy(impactVfxInstance.gameObject, ImpactVfxLifetime);
                }
            }

            // 声音
            if (ImpactSfxClip)
            {
                m_AudioSource.PlayOneShot(ImpactSfxClip);
            }

            // Self Destruct
            Destroy(this.gameObject);
        }
    }

}
