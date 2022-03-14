using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.FPS.Gameplay;
using UnityEngine.Events;

namespace Unity.FPS.Player
{
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerWeaponsManager : MonoBehaviour
    {
        public enum WeaponSwitchState
        {
            Up,//正常情况
            Down,//无枪情况
            PutDownPrevious,//放下旧枪械
            PutUpNew,//抬起新枪械
        }

        [Tooltip("武器列表")]
        public List<WeaponController> StartingWeapons = new List<WeaponController>();
        [Tooltip("武器摄像头Depth only+layer")]
        public Camera WeaponCamera;
        [Tooltip("武器添加父节点,用于Pick时的武器添加")]
        public Transform WeaponParentSocket;
        [Tooltip("武器瞄准时的切换位置")]
        public Transform AimingWeaponPosition;
        [Tooltip("非瞄准时的参考位置")]
        public Transform DefaultWeaponPosition;
        [Tooltip("切换武器时的下移位置")]
        public Transform DownWeaponPosition;
        
        [Header("武器的动态摆动")]
        [Tooltip("人物移动时的武器摆动频率")]
        public float BobFrequency = 10f;
        [Tooltip("人物移动时的武器摆动速率")]
        public float BobSharpness = 10f;
        [Tooltip("未瞄准时的移动摆动距离")]
        public float DefaultBobAmount = 0.05f;
        [Tooltip("瞄准时的移动摆动距离")]
        public float AimingBobAmount = 0.02f;
        [Tooltip("瞄准时的视角切换速度")]
        public float AimingAnimationSpeed = 10f;

        [Header("武器的后座力")]
        [Tooltip("产生后座力的相应速度")]
        public float RecoilSharpness = 50f;
        [Tooltip("产生后座力的最大距离")]
        public float MaxRecoilDistance = 0.5f;
        [Tooltip("返回初始姿态时的速度")]
        public float RecoilRestitutionSharpness = 10f;

        [Header("显示设置")]
        [Tooltip("摄像头的默认视距")]
        public float DefaultFov = 60f;
        [Tooltip("武器摄像头显示的图层")]
        public LayerMask FpsWeaponLayer;

        public bool IsAiming { get; private set; }//是否处于瞄准状态
        public bool IsPointingAtEnemy { get; private set; }//是否有瞄准对象
        public int ActiveWeaponIndex { get; private set; }//当前武器Index



        //联合UI界面层进行武器的动态切换等效果
        public UnityAction<WeaponController> OnSwitchedToWeapon;
        public UnityAction<WeaponController, int> OnAddedWeapon;
        public UnityAction<WeaponController, int> OnRemovedWeapon;

        WeaponController[] m_WeaponSlots = new WeaponController[6];//可装载的武器数
        PlayerInputHandler m_InputHandler;//输入单元
        PlayerCharacterController m_PlayerCharacterController;

        Vector3 m_WeaponMainLocalPosition;//武器的位置
        Vector3 m_AccumulatedRecoil;//积累的武器后坐力
        Vector3 m_WeaponBobLocalPosition;//武器的当前帧所处位置，用于控制晃动
        Vector3 m_WeaponRecoilLocalPosition;//武器的当前帧下的后移距离，用于控制后座力效果
        Vector3 m_LastCharacterPosition;//武器的上一帧所处位置，综合了晃动和后座力的叠加态效果

        float m_WeaponBobFactor;//武器晃动速率

        WeaponSwitchState m_WeaponSwitchState;//武器管理状态
        int m_WeaponSwitchNewWeaponIndex;//即将切换的新武器序号
        float m_TimeStartedWeaponSwitch;//武器切换的开始时间
        


        // Start is called before the first frame update
        void Start()
        {
            ActiveWeaponIndex = -1;//初始为无武器的状态
            IsAiming = false;
            m_InputHandler = GetComponent<PlayerInputHandler>();
            m_PlayerCharacterController = GetComponent<PlayerCharacterController>();

            m_WeaponSwitchState = WeaponSwitchState.Down;//无持枪
            SetFov(DefaultFov);

            OnSwitchedToWeapon += OnWeaponSwitched;

            foreach (var weapon in StartingWeapons)
            {
                AddWeapon(weapon);
            }
            //切换至第一个加载的武器（正序遍历就是第一个）
            SwitchWeapon(true);
        }

        // Update is called once per frame
        void Update()
        {
            WeaponController activeWeapon = GetActiveWeapon();

            if (activeWeapon != null && activeWeapon.IsAnimator())
                return;

            if (activeWeapon != null && m_WeaponSwitchState == WeaponSwitchState.Up)
            {
                //加子弹
                if (m_InputHandler.GetReloadButtonDown() && activeWeapon.GetCurrentAmmoRatio() < 1.0f&& !activeWeapon.IsReloading)
                {
                    IsAiming = false;
                    activeWeapon.StartReloadAnimation();
                    return;
                }
                //开启瞄准
                IsAiming = m_InputHandler.GetAimInputHeld();
                //判断是否处于开枪转台
                bool hasFired = activeWeapon.HandleShootInputs(
                    m_InputHandler.GetFireInputDown(),
                    m_InputHandler.GetFireInputHeld(),
                    m_InputHandler.GetFireInputReleased());

                if (hasFired)
                {
                    //在此处待添加枪械的随机攻击位置0313

                    m_AccumulatedRecoil += Vector3.back;
                    //防止超出最大后置区域
                    m_AccumulatedRecoil = Vector3.ClampMagnitude(m_AccumulatedRecoil, MaxRecoilDistance);
                }
            }
            //换枪
            if (!IsAiming &&(m_WeaponSwitchState == WeaponSwitchState.Up || m_WeaponSwitchState == WeaponSwitchState.Down))
            {
                int switchWeaponInput = m_InputHandler.GetSwitchWeaponInput();//滚轮输出
                if (switchWeaponInput != 0)
                {
                    bool switchUp = switchWeaponInput > 0;
                    SwitchWeapon(switchUp);
                }
                else
                {
                    switchWeaponInput = m_InputHandler.GetSelectWeaponInput();//数字键盘输出选择
                    if (switchWeaponInput != 0)
                    {
                        if (GetWeaponAtSlotIndex(switchWeaponInput - 1) != null)
                            SwitchToWeaponIndex(switchWeaponInput - 1);
                    }
                }
            }

            IsPointingAtEnemy = false;
            if (activeWeapon)
            {
                if (Physics.Raycast(WeaponCamera.transform.position, WeaponCamera.transform.forward, out RaycastHit hit,
                    1000, -1, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.GetComponentInParent<Health>() != null)
                    {
                        IsPointingAtEnemy = true;//射线寻敌
                    }
                }
            }
        }
        //LateUpdate在所有Update完成后被调用，一般用于处理新频率要求比较稳定的物理系统
        private void LateUpdate()
        {
            UpdateWeaponAiming();
            UpdateWeaponBob();
            UpdateWeaponRecoil();
            UpdateWeaponSwitching();

            WeaponParentSocket.localPosition =
                m_WeaponMainLocalPosition + m_WeaponBobLocalPosition + m_WeaponRecoilLocalPosition;
        }


        public WeaponController GetActiveWeapon()
        {
            return GetWeaponAtSlotIndex(ActiveWeaponIndex);
        }

        public WeaponController GetWeaponAtSlotIndex(int index)
        {
            if (index >= 0 &&
                index < m_WeaponSlots.Length)
            {
                return m_WeaponSlots[index];
            }
            return null;
        }
        /// <summary>
        /// 设置当前Camera和WeaponCamera
        /// </summary>
        /// <param name="fov">视距，默认60</param>
        public void SetFov(float fov)
        {
            //同步切换
            m_PlayerCharacterController.PlayerCamera.fieldOfView = fov;
            WeaponCamera.fieldOfView = fov;
        }

        void OnWeaponSwitched(WeaponController newWeapon)
        {
            if (newWeapon != null)
            {
                newWeapon.ShowWeapon(true);
            }
        }
        /// <summary>
        /// 添加一个新的武器
        /// </summary>
        /// <param name="weaponPrefab">当前武器的预制体，需要有WeaponController组件</param>
        /// <returns>是否完成</returns>
        public bool AddWeapon(WeaponController weaponPrefab)
        {
            if (HasWeapon(weaponPrefab))
                return false;
            //替换无效的枪支
            for (int i = 0; i < m_WeaponSlots.Length; i++)
            {
                // only add the weapon if the slot is free
                if (m_WeaponSlots[i] == null)
                {
                    WeaponController weaponInstance = Instantiate(weaponPrefab, WeaponParentSocket);
                    weaponInstance.transform.localPosition = Vector3.zero;
                    weaponInstance.transform.localRotation = Quaternion.identity;

                    weaponInstance.Owner = gameObject;
                    weaponInstance.SourcePrefab = weaponPrefab.gameObject;
                    weaponInstance.ShowWeapon(false);

                    // 对该武器定义指定显示图层
                    int layerIndex =Mathf.RoundToInt(Mathf.Log(FpsWeaponLayer.value,2));
                    foreach (Transform t in weaponInstance.gameObject.GetComponentsInChildren<Transform>(true))
                    {
                        t.gameObject.layer = layerIndex;
                    }

                    m_WeaponSlots[i] = weaponInstance;
                    if (OnAddedWeapon != null)
                    {
                        OnAddedWeapon.Invoke(weaponInstance, i);
                    }
                    return true;
                }
            }
            //添加失败
            return false;
        }

        public WeaponController HasWeapon(WeaponController weaponPrefab)
        {
            // Checks if we already have a weapon coming from the specified prefab
            for (var index = 0; index < m_WeaponSlots.Length; index++)
            {
                var w = m_WeaponSlots[index];
                if (w != null && w.SourcePrefab == weaponPrefab.gameObject)
                {
                    return w;
                }
            }

            return null;
        }
        /// <summary>
        /// 用于切换当前武器，基于形参判断其正序或反序
        /// </summary>
        /// <param name="ascendingOrder">true正序/false反序</param>
        public void SwitchWeapon(bool ascendingOrder)
        {
            int newWeaponIndex = -1;
            int closestSlotDistance = m_WeaponSlots.Length;
            for (int i = 0; i < m_WeaponSlots.Length; i++)
            {
                if (i != ActiveWeaponIndex && GetWeaponAtSlotIndex(i) != null)
                {
                    int distanceToActiveIndex = GetDistanceBetweenWeaponSlots(ActiveWeaponIndex, i, ascendingOrder);

                    if (distanceToActiveIndex < closestSlotDistance)
                    {
                        closestSlotDistance = distanceToActiveIndex;
                        newWeaponIndex = i;
                    }
                }
            }
            SwitchToWeaponIndex(newWeaponIndex);
        }

        public void SwitchToWeaponIndex(int newWeaponIndex, bool force = false)
        {
            if (force || (newWeaponIndex != ActiveWeaponIndex && newWeaponIndex >= 0))
            {
                m_WeaponSwitchNewWeaponIndex = newWeaponIndex;
                m_TimeStartedWeaponSwitch = Time.time;

                //当前无武器，抬起新武器
                if (GetActiveWeapon() == null)
                {
                    m_WeaponMainLocalPosition = DownWeaponPosition.localPosition;
                    m_WeaponSwitchState = WeaponSwitchState.PutUpNew;
                    ActiveWeaponIndex = m_WeaponSwitchNewWeaponIndex;

                    WeaponController newWeapon = GetWeaponAtSlotIndex(m_WeaponSwitchNewWeaponIndex);
                    if (OnSwitchedToWeapon != null)
                    {
                        OnSwitchedToWeapon.Invoke(newWeapon);
                    }
                    //newWeapon.StartPutUpAnimation();
                }
                // 放下当前武器，并切换当前武器状态
                else
                {
                    //GetActiveWeapon().StartPutDownAnimation();
                    m_WeaponSwitchState = WeaponSwitchState.PutDownPrevious;
                }
            }
        }

        int GetDistanceBetweenWeaponSlots(int fromSlotIndex, int toSlotIndex, bool ascendingOrder)
        {
            int distanceBetweenSlots;
            if (ascendingOrder)
                distanceBetweenSlots = toSlotIndex - fromSlotIndex;//正序
            else
                distanceBetweenSlots = -1 * (toSlotIndex - fromSlotIndex);//反序
            if (distanceBetweenSlots < 0)
                distanceBetweenSlots = m_WeaponSlots.Length + distanceBetweenSlots;//反置
            return distanceBetweenSlots;
        }
        //完成瞄准的视角切换
        void UpdateWeaponAiming()
        {
            if (m_WeaponSwitchState == WeaponSwitchState.Up)
            {
                WeaponController activeWeapon = GetActiveWeapon();
                if (IsAiming && activeWeapon != null)
                {
                    //切换武器至指定位置
                    m_WeaponMainLocalPosition = Vector3.Lerp(m_WeaponMainLocalPosition,
                        AimingWeaponPosition.localPosition + activeWeapon.AimOffset,
                        AimingAnimationSpeed * Time.deltaTime);
                    //同步缓慢切换camera
                    SetFov(Mathf.Lerp(m_PlayerCharacterController.PlayerCamera.fieldOfView,
                        activeWeapon.AimZoomRatio * DefaultFov, AimingAnimationSpeed * Time.deltaTime));
                }
                else
                {
                    m_WeaponMainLocalPosition = Vector3.Lerp(m_WeaponMainLocalPosition,
                        DefaultWeaponPosition.localPosition, AimingAnimationSpeed * Time.deltaTime);
                    SetFov(Mathf.Lerp(m_PlayerCharacterController.PlayerCamera.fieldOfView, DefaultFov,
                        AimingAnimationSpeed * Time.deltaTime));
                }
            }
        }
        /// <summary>
        /// 更新当前武器在随着角色移动时的位置切换
        /// </summary>
        void UpdateWeaponBob()
        {
            if (Time.deltaTime > 0f)
            {
                Vector3 playerCharacterVelocity =
                    (m_PlayerCharacterController.transform.position - m_LastCharacterPosition) / Time.deltaTime;

                float characterMovementFactor = 0f;
                if (m_PlayerCharacterController.IsGrounded)
                {
                    characterMovementFactor =
                        Mathf.Clamp01(playerCharacterVelocity.magnitude /
                                      (m_PlayerCharacterController.MaxSpeedOnGround *
                                       m_PlayerCharacterController.SprintSpeedModifier));
                }

                m_WeaponBobFactor =
                    Mathf.Lerp(m_WeaponBobFactor, characterMovementFactor, BobSharpness * Time.deltaTime);

                // Calculate vertical and horizontal weapon bob values based on a sine function
                float bobAmount = IsAiming ? AimingBobAmount : DefaultBobAmount;
                float frequency = BobFrequency;
                float hBobValue = Mathf.Sin(Time.time * frequency) * bobAmount * m_WeaponBobFactor;
                float vBobValue = ((Mathf.Sin(Time.time * frequency * 2f) * 0.5f) + 0.5f) * bobAmount *
                                  m_WeaponBobFactor;

                m_WeaponBobLocalPosition.x = hBobValue;
                m_WeaponBobLocalPosition.y = Mathf.Abs(vBobValue);

                m_LastCharacterPosition = m_PlayerCharacterController.transform.position;
            }
        }

        //处理发射时的后座力
        void UpdateWeaponRecoil()
        {
            if (m_WeaponRecoilLocalPosition.z >= m_AccumulatedRecoil.z * 0.99f)
            {
                //已到最大值
                m_WeaponRecoilLocalPosition = Vector3.Lerp(m_WeaponRecoilLocalPosition, m_AccumulatedRecoil,
                    RecoilSharpness * Time.deltaTime);
            }
            else
            {
                //开始往回缓冲
                m_WeaponRecoilLocalPosition = Vector3.Lerp(m_WeaponRecoilLocalPosition, Vector3.zero,
                    RecoilRestitutionSharpness * Time.deltaTime);
                m_AccumulatedRecoil = m_WeaponRecoilLocalPosition;
            }
        }

        void UpdateWeaponSwitching()
        {
            float switchingTimeFactor = Mathf.Clamp01((Time.time - m_TimeStartedWeaponSwitch)/0.2f);//已累计的切枪时间
            if (switchingTimeFactor >= 1f)
            {
                if (m_WeaponSwitchState == WeaponSwitchState.PutDownPrevious)
                {
                    WeaponController oldWeapon = GetWeaponAtSlotIndex(ActiveWeaponIndex);
                    if (oldWeapon != null)
                    {
                        //隐藏
                        oldWeapon.ShowWeapon(false);
                    }

                    ActiveWeaponIndex = m_WeaponSwitchNewWeaponIndex;
                    switchingTimeFactor = 0f;

                    WeaponController newWeapon = GetWeaponAtSlotIndex(ActiveWeaponIndex);
                    if (OnSwitchedToWeapon != null)
                    {
                        OnSwitchedToWeapon.Invoke(newWeapon);//UI动画
                    }
                    if (newWeapon)
                    {
                        m_TimeStartedWeaponSwitch = Time.time;//重置刷新时间，准备抬枪
                        m_WeaponSwitchState = WeaponSwitchState.PutUpNew;
                    }
                    else
                    {
                        // 无枪械可进行更换
                        m_WeaponSwitchState = WeaponSwitchState.Down;
                    }
                }
                else if (m_WeaponSwitchState == WeaponSwitchState.PutUpNew)
                {
                    m_WeaponSwitchState = WeaponSwitchState.Up;
                }
            }
            if (m_WeaponSwitchState == WeaponSwitchState.PutDownPrevious)
            {
                m_WeaponMainLocalPosition = Vector3.Lerp(DefaultWeaponPosition.localPosition,
                    DownWeaponPosition.localPosition, switchingTimeFactor);
            }
            else if (m_WeaponSwitchState == WeaponSwitchState.PutUpNew)
            {
                m_WeaponMainLocalPosition = Vector3.Lerp(DownWeaponPosition.localPosition,
                    DefaultWeaponPosition.localPosition, switchingTimeFactor);
            }
        }
    }
}
