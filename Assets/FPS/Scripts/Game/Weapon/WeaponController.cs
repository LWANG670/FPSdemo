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
        Charge//充能
    }

    public struct CrosshairData
    {
        [Tooltip("Sprite的瞄准贴图")]
        public Sprite CrosshairSprite;

        [Tooltip("瞄准大小")]
        public int CrosshairSize;

        [Tooltip("显示的颜色")]
        public Color CrosshairColor;
    }
    //管理武器本体的效果
    [RequireComponent(typeof(AudioSource))]
    public class WeaponController : MonoBehaviour
    {
        [Tooltip("武器图标")]
        public Sprite WeaponIcon;
        [Tooltip("默认瞄准")]
        public CrosshairData CrosshairDataDefault;
        [Tooltip("当瞄准到对象时")]
        public CrosshairData CrosshairDataTargetInSight;

        [Tooltip("瞄准时的镜头缩放")]
        [Range(0f, 1f)]
        public float AimZoomRatio = 0.5f;
        [Tooltip("瞄准时的距离补偿")]
        public Vector3 AimOffset;

        [Header("武器模型及子弹模块")]
        [Tooltip("武器模型本身")]
        public GameObject WeaponRoot;
        [Tooltip("子弹发射位置")]
        public Transform WeaponMuzzle;
        [Tooltip("武器攻击方式（单发/连发/蓄力（待完成））")]
        public WeaponShootType ShootType;
        

        [Header("子弹模块")]
        [Tooltip("子弹预制体")] 
        public ProjectileBase ProjectilePrefab;
        [Tooltip("发射时间间隔")]
        public float DelayBetweenShots = 0.5f;
        [Tooltip("发射角度偏移")]
        public float BulletSpreadAngle = 0f;
        [Tooltip("每次发射的数量（用于散弹枪）")]
        public int BulletsPerShot = 1;

        [Tooltip("自动装弹")]
        public bool AutomaticReload = true;
        [Tooltip("自动装弹的延时")]
        public float AmmoReloadDelay = 2f;
        [Tooltip("子弹填充数")]
        public int MaxAmmo = 30;

        [Header("子弹弹出效果")]
        public bool HasPhysicalBullets = false;
        [Tooltip("子弹弹出预制体模型")]
        public GameObject ShellCasing;
        [Tooltip("子弹弹出位置")]
        public Transform EjectionPort;
        [Tooltip("弹出的力")]
        [Range(0.0f, 5.0f)] 
        public float ShellCasingEjectionForce = 2.0f;

        [Header("音效/动画")]
        [Tooltip("武器动画机")]
        public Animator WeaponAnimator;
        [Tooltip("发射时的闪光预制体")]
        public GameObject MuzzleFlashPrefab;
        [Tooltip("发射音效")]
        public AudioClip ShootSfx;
        [Tooltip("切换武器音效")]
        public AudioClip ChangeWeaponSfx;

        public GameObject Owner { get; set; }//判断当前子弹的拥有者（Player或者Enemy）
        public GameObject SourcePrefab { get; set; }//当前武器的预制体
        public bool IsReloading { get; private set; }//是否处于装弹时刻

        public bool IsPutUping { get; private set; }

        public bool IsPutDowning { get; private set; }

        public bool IsWeaponActive { get; private set; }//当前武器是否有效

        public UnityAction OnShoot;

        int m_CurrentAmmo;//当前子弹填充量
        float m_LastTimeShot = Mathf.NegativeInfinity;//最后发射的时间
        Vector3 m_LastMuzzlePosition;//最后发射的位置
        AudioSource m_AudioSource;

        //动画触发器名称
        const string k_AnimPutUpParameter = "PutUp";
        const string k_AnimPutDownParameter = "PutDown";
        const string k_AnimAttackParameter = "Attack";//00:16秒
        const string k_AnimReloadParameter = "Reload";//02:16秒
        const string k_AniSprintParameter = "Sprint";//冲刺

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
                Invoke("FinishReload", 3.2f);//延时两秒完成装弹
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
            Invoke("FinishPutUp", 1f);//延时两秒完成装弹
        }
        void FinishPutDown()
        {
            IsPutDowning = false;
        }
        public void StartPutDownAnimation()
        {
            GetComponent<Animator>().SetTrigger(k_AnimPutDownParameter);
            IsPutDowning = true;
            Invoke("FinishPutDown", 1f);//延时两秒完成装弹
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
                //增加一个瞬时力
                shellRigi.AddForce(nextShell.transform.up * ShellCasingEjectionForce, ForceMode.Impulse);
            }
            Destroy(nextShell, 2f);
        }

        void UpdateAmmo()
        {
            if (AutomaticReload && m_LastTimeShot + AmmoReloadDelay < Time.time&& m_CurrentAmmo == 0 && !IsReloading)
            {
                //自动装弹
                StartReloadAnimation();
            }
        }
        //控制子弹减少数
        public void UseAmmo(int amount)
        {
            m_CurrentAmmo = Mathf.Clamp(m_CurrentAmmo - amount, 0, MaxAmmo);
            m_LastTimeShot = Time.time;
        }

        /// <summary>
        /// 发射控制
        /// </summary>
        /// <param name="inputDown">鼠标摁下</param>
        /// <param name="inputHeld">鼠标摁下后的保持</param>
        /// <param name="inputUp">鼠标抬起</param>
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
                        return TryShoot();//单发
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
        //尝试开枪
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
                newProjectile.Shoot(this);//子弹开启
            }

            // 开火特效
            if (MuzzleFlashPrefab != null)
            {
                GameObject muzzleFlashInstance = Instantiate(MuzzleFlashPrefab, WeaponMuzzle.position,
                    WeaponMuzzle.rotation, WeaponMuzzle.transform);

                Destroy(muzzleFlashInstance, 2f);
            }

            if (HasPhysicalBullets)
            {
                ShootShell();//弹壳弹出特效
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

        //发射方向的随机处理
        public Vector3 GetShotDirectionWithinSpread(Transform shootTransform)
        {
            float spreadAngleRatio = BulletSpreadAngle / 180f;
            Vector3 spreadWorldDirection = Vector3.Slerp(shootTransform.forward, UnityEngine.Random.insideUnitSphere,
                spreadAngleRatio);

            return spreadWorldDirection;
        }
    }
}
