using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Gameplay
{
    public enum WeaponShootType
    {
        Manual,
        Automatic,
        Charge//����
    }

    public struct CrosshairData
    {
        [Tooltip("Sprite����׼��ͼ")]
        public Sprite CrosshairSprite;

        [Tooltip("��׼��С")]
        public int CrosshairSize;

        [Tooltip("��ʾ����ɫ")]
        public Color CrosshairColor;
    }
    //�������������Ч��
    [RequireComponent(typeof(AudioSource))]
    public class WeaponController : MonoBehaviour
    {
        [Tooltip("����ͼ��")]
        public Sprite WeaponIcon;
        [Tooltip("Ĭ����׼")]
        public CrosshairData CrosshairDataDefault;
        [Tooltip("����׼������ʱ")]
        public CrosshairData CrosshairDataTargetInSight;

        [Tooltip("��׼ʱ�ľ�ͷ����")]
        [Range(0f, 1f)]
        public float AimZoomRatio = 0.5f;
        [Tooltip("��׼ʱ�ľ��벹��")]
        public Vector3 AimOffset;

        [Header("����ģ�ͼ��ӵ�ģ��")]
        [Tooltip("����ģ�ͱ���")]
        public GameObject WeaponRoot;
        [Tooltip("�ӵ�����λ��")]
        public Transform WeaponMuzzle;
        [Tooltip("����������ʽ������/����/����������ɣ���")]
        public WeaponShootType ShootType;
        

        [Header("�ӵ�ģ��")]
        [Tooltip("�ӵ�Ԥ����")] 
        public ProjectileBase ProjectilePrefab;
        [Tooltip("����ʱ����")]
        public float DelayBetweenShots = 0.5f;
        [Tooltip("����Ƕ�ƫ��")]
        public float BulletSpreadAngle = 0f;
        [Tooltip("ÿ�η��������������ɢ��ǹ��")]
        public int BulletsPerShot = 1;

        [Tooltip("�Զ�װ��")]
        public bool AutomaticReload = true;
        [Tooltip("�Զ�װ������ʱ")]
        public float AmmoReloadDelay = 2f;
        [Tooltip("�ӵ������")]
        public int MaxAmmo = 30;

        [Header("�ӵ�����Ч��")]
        public bool HasPhysicalBullets = false;
        [Tooltip("�ӵ�����Ԥ����ģ��")]
        public GameObject ShellCasing;
        [Tooltip("�ӵ�����λ��")]
        public Transform EjectionPort;
        [Tooltip("��������")]
        [Range(0.0f, 5.0f)] 
        public float ShellCasingEjectionForce = 2.0f;

        [Header("��Ч/����")]
        [Tooltip("����������")]
        public Animator WeaponAnimator;
        [Tooltip("����ʱ������Ԥ����")]
        public GameObject MuzzleFlashPrefab;
        [Tooltip("������Ч")]
        public AudioClip ShootSfx;
        [Tooltip("�л�������Ч")]
        public AudioClip ChangeWeaponSfx;

        public GameObject Owner { get; set; }//�жϵ�ǰ�ӵ���ӵ���ߣ�Player����Enemy��
        public GameObject SourcePrefab { get; set; }//��ǰ������Ԥ����
        public bool IsReloading { get; private set; }//�Ƿ���װ��ʱ��

        public bool IsPutUping { get; private set; }

        public bool IsPutDowning { get; private set; }

        public bool IsWeaponActive { get; private set; }//��ǰ�����Ƿ���Ч

        public UnityAction OnShoot;

        int m_CurrentAmmo;//��ǰ�ӵ������
        float m_LastTimeShot = Mathf.NegativeInfinity;//������ʱ��
        Vector3 m_LastMuzzlePosition;//������λ��
        AudioSource m_AudioSource;

        //��������������
        const string k_AnimPutUpParameter = "PutUp";
        const string k_AnimPutDownParameter = "PutDown";
        const string k_AnimAttackParameter = "Attack";//00:16��
        const string k_AnimReloadParameter = "Reload";//02:16��
        const string k_AniSprintParameter = "Sprint";//���

        public float GetCurrentAmmoRatio() => (float)m_CurrentAmmo / MaxAmmo;
        public bool IsAnimator() => IsPutDowning || IsReloading || IsPutUping;
        public int GetCurrentAmmo() => Mathf.FloorToInt(m_CurrentAmmo);
        private void Awake()
        {
            m_CurrentAmmo = MaxAmmo;
            
        }
        void Start()
        {
            m_LastMuzzlePosition = WeaponMuzzle.position;
            m_AudioSource = GetComponent<AudioSource>();
        }

        // Update is called once per frame
        void Update()
        {
            UpdateAmmo();
            if (Time.deltaTime > 0)
            {
                m_LastMuzzlePosition = WeaponMuzzle.position;
            }
        }

        void FinishReload()
        {
            m_CurrentAmmo = MaxAmmo;
            IsReloading = false;
        }

        public void StartReloadAnimation()
        {
            if (m_CurrentAmmo < MaxAmmo)
            {
                GetComponent<Animator>().SetTrigger(k_AnimReloadParameter);
                IsReloading = true;
                Invoke("FinishReload", 3.2f);//��ʱ�������װ��
            }
        }

        void FinishPutUp()
        {
            IsPutUping = false;
        }
        public void StartPutUpAnimation()
        {
            GetComponent<Animator>().SetTrigger(k_AnimPutUpParameter);
            IsPutUping = true;
            Invoke("FinishPutUp", 1f);//��ʱ�������װ��
        }
        void FinishPutDown()
        {
            IsPutDowning = false;
        }
        public void StartPutDownAnimation()
        {
            GetComponent<Animator>().SetTrigger(k_AnimPutDownParameter);
            IsPutDowning = true;
            Invoke("FinishPutDown", 1f);//��ʱ�������װ��
        }

        public void StartSprintAnimation()
        {
            GetComponent<Animator>().SetTrigger(k_AniSprintParameter);
        }
        

        public void ShowWeapon(bool show)
        {
            WeaponRoot.SetActive(show);

            if (show && ChangeWeaponSfx)
            {
                m_AudioSource.PlayOneShot(ChangeWeaponSfx);
                StartPutUpAnimation();
            }

            IsWeaponActive = show;
        }

        void ShootShell()
        {
            GameObject nextShell = Instantiate(ShellCasing, transform);
            //Rigidbody nextShell = m_PhysicalAmmoPool.Dequeue();
            Rigidbody shellRigi = nextShell.GetComponent<Rigidbody>();
            if (shellRigi)
            {
                shellRigi.transform.position = EjectionPort.transform.position;
                shellRigi.transform.rotation = EjectionPort.transform.rotation;
                shellRigi.gameObject.SetActive(true);
                shellRigi.transform.SetParent(null);
                shellRigi.collisionDetectionMode = CollisionDetectionMode.Continuous;
                //����һ��˲ʱ��
                shellRigi.AddForce(nextShell.transform.up * ShellCasingEjectionForce, ForceMode.Impulse);
            }
            Destroy(nextShell, 2f);
        }

        void UpdateAmmo()
        {
            if (AutomaticReload && m_LastTimeShot + AmmoReloadDelay < Time.time&& m_CurrentAmmo == 0 && !IsReloading)
            {
                //�Զ�װ��
                StartReloadAnimation();
            }
        }
        //�����ӵ�������
        public void UseAmmo(int amount)
        {
            m_CurrentAmmo = Mathf.Clamp(m_CurrentAmmo - amount, 0, MaxAmmo);
            m_LastTimeShot = Time.time;
        }

        /// <summary>
        /// �������
        /// </summary>
        /// <param name="inputDown">�������</param>
        /// <param name="inputHeld">������º�ı���</param>
        /// <param name="inputUp">���̧��</param>
        /// <returns></returns>
        public bool HandleShootInputs(bool inputDown, bool inputHeld, bool inputUp)
        {
            if (IsAnimator())
                return false;
            switch (ShootType)
            {
                case WeaponShootType.Manual:
                    if (inputDown)
                    {
                        return TryShoot();//����
                    }
                    return false;
                case WeaponShootType.Automatic:
                    if (inputHeld)
                    {
                        return TryShoot();
                    }
                    return false;
                case WeaponShootType.Charge:
                    

                    return false;

                default:
                    return false;
            }
        }
        //���Կ�ǹ
        bool TryShoot()
        {
            if (m_CurrentAmmo >= 1
                && m_LastTimeShot + DelayBetweenShots < Time.time)
            {
                HandleShoot();
                m_CurrentAmmo -= 1;

                return true;
            }

            return false;
        }

        void HandleShoot()
        {
            int bulletsPerShotFinal = BulletsPerShot;

            // spawn all bullets with random direction
            for (int i = 0; i < bulletsPerShotFinal; i++)
            {
                Vector3 shotDirection = GetShotDirectionWithinSpread(WeaponMuzzle);
                ProjectileBase newProjectile = Instantiate(ProjectilePrefab, WeaponMuzzle.position,
                    Quaternion.LookRotation(shotDirection));
                newProjectile.Shoot(this);//�ӵ�����
            }

            // ������Ч
            if (MuzzleFlashPrefab != null)
            {
                GameObject muzzleFlashInstance = Instantiate(MuzzleFlashPrefab, WeaponMuzzle.position,
                    WeaponMuzzle.rotation, WeaponMuzzle.transform);

                Destroy(muzzleFlashInstance, 2f);
            }

            if (HasPhysicalBullets)
            {
                ShootShell();//���ǵ�����Ч
            }

            m_LastTimeShot = Time.time;

            if (ShootSfx&& m_AudioSource)
            {
                m_AudioSource.PlayOneShot(ShootSfx);
            }

            if (WeaponAnimator)
            {
                WeaponAnimator.SetTrigger(k_AnimAttackParameter);
            }

            OnShoot?.Invoke();
        }

        //���䷽����������
        public Vector3 GetShotDirectionWithinSpread(Transform shootTransform)
        {
            float spreadAngleRatio = BulletSpreadAngle / 180f;
            Vector3 spreadWorldDirection = Vector3.Slerp(shootTransform.forward, UnityEngine.Random.insideUnitSphere,
                spreadAngleRatio);

            return spreadWorldDirection;
        }
    }
}
