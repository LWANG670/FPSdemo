using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Player;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    //�ӵ���������..ֱ��/����
    public enum BulletType
    {
        Standard,
        Seeker
    }
    //�ӵ���������
    public enum DamageType
    {
        Direct,
        Explosion
    }
    public class ProjectileStandard : ProjectileBase
    {
        [Tooltip("�ӵ�����:ֱ�߻��߸���")]
        public BulletType BulletType = BulletType.Standard;
        [Tooltip("�ӵ��˺�����:ֱ�߻��߱�ը�˺�")]
        public DamageType DamageType = DamageType.Direct;
        [Tooltip("�ӵ�����ʱ��")]
        public float MaxLifeTime = 5f;

        [Header("����ģ��")]
        [Tooltip("��ײ������ʱ������Ч��")]
        public GameObject ImpactVfx;
        [Tooltip("����Ч������ʱ��")]
        public float ImpactVfxLifetime = 5f;
        [Tooltip("����ƫ��")]
        public float ImpactVfxSpawnOffset = 0.1f;

        [Header("Damageģ��")]
        [Tooltip("������Ŀ���Layer")]
        public LayerMask HittableLayers = -1;
        [Tooltip("�ӵ��ٶ�")]
        public float Speed = 20f;
        [Tooltip("�ӵ��˺�")]
        public float Damage = 1f;
        [Tooltip("���е���Ч")]
        public AudioClip ImpactSfxClip;
        

        [Header("Ѱ��Ч��")]
        [Tooltip("Ѱ������ʱ����")]
        public float TargetListUpdateRate = 1.0f;

        [Header("��ȷ��ײ")]
        [Tooltip("�ӵ��뾶")]
        public float Radius = 0.01f;
        [Tooltip("�ӵ���ʼ��λ��")]
        public Transform Root;
        [Tooltip("�ӵ�ͷ����λ��")]
        public Transform Tip;


        [Header("��ըЧ��")]
        [Tooltip("��ը��")]
        public float ExplosionForce = 5.0f;
        [Tooltip("��ը�뾶")]
        public float ExplosionRadius = 10.0f;

        [Header("��Ч��")]
        [Tooltip("����Ч��")]
        public float GravityDownAcceleration = 0f;



        ProjectileBase m_ProjectileBase;//���浱ǰ�ӵ����������ԣ��������ȣ�
        Vector3 m_Velocity;//�������ƶ�����
        Vector3 m_LastRootPosition;
        GameObject TargetObject;//Ѱ��Ŀ��
        List<Collider> m_IgnoredColliders;//�����ӵĶ���
        AudioSource m_AudioSource;

        //OnEnable������Awake,Ҫ����ʱ�ſ�ʼִ�У�������Start
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
            //����
            if (GravityDownAcceleration > 0)
            {
                // add gravity to the projectile velocity for ballistic effect
                m_Velocity += Vector3.down * GravityDownAcceleration * Time.deltaTime;
            }
            //����Ը��ٵ������п���
            {

            }
            //��������
            {
                //RatcastHit���ڴ洢�������ߺ��������ײ��Ϣ
                RaycastHit closestHit = new RaycastHit();
                closestHit.distance = Mathf.Infinity;
                bool foundHit = false;

                Vector3 displacementSinceLastFrame = Tip.position - m_LastRootPosition;//�γ�һ����Բ�������ײ��

                RaycastHit[] hits = Physics.SphereCastAll(m_LastRootPosition, Radius,
                    displacementSinceLastFrame.normalized, displacementSinceLastFrame.magnitude, HittableLayers,
                    QueryTriggerInteraction.Collide);
                //Ѱ���������ײ��
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
            m_LastRootPosition = Root.position;//������һ��λ��
        }

        //���ظ���̳�
        new void OnShoot()
        {
            //�������
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
        //���鵱ǰ��ײ��Ч��
        bool IsHitValid(RaycastHit hit)
        {
            if (hit.collider.GetComponent<IgnoreHitDetection>())
                return false;
            //�����յ��˺�
            if (hit.collider.isTrigger && hit.collider.GetComponent<Damageable>() == null)
                return false;
            if (m_IgnoredColliders != null && m_IgnoredColliders.Contains(hit.collider))
                return false;
            return true;
        }

        //����Ŀ���
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
                //��õ�ǰ��������������
                Collider[] cols = Physics.OverlapSphere(transform.position, ExplosionRadius);
                //Ϊ������ɫʩ����
                List<Rigidbody> rigidbodies = new List<Rigidbody>();
                foreach (Collider col in cols)
                {
                    //��Χ�˺�����
                    float damageAmount = Damage * (1 / Vector3.Distance(transform.position, col.transform.position));
                    Damageable damageable = collider.GetComponent<Damageable>();
                    if (damageable)
                    {
                        damageable.InflictDamage(Damage, false, m_ProjectileBase.Owner);
                    }
                    //������ը��
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

            // ����
            if (ImpactSfxClip)
            {
                m_AudioSource.PlayOneShot(ImpactSfxClip);
            }

            // Self Destruct
            Destroy(this.gameObject);
        }
    }

}
