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
            Up,//�������
            Down,//��ǹ���
            PutDownPrevious,//���¾�ǹе
            PutUpNew,//̧����ǹе
        }

        [Tooltip("�����б�")]
        public List<WeaponController> StartingWeapons = new List<WeaponController>();
        [Tooltip("��������ͷDepth only+layer")]
        public Camera WeaponCamera;
        [Tooltip("������Ӹ��ڵ�,����Pickʱ���������")]
        public Transform WeaponParentSocket;
        [Tooltip("������׼ʱ���л�λ��")]
        public Transform AimingWeaponPosition;
        [Tooltip("����׼ʱ�Ĳο�λ��")]
        public Transform DefaultWeaponPosition;
        [Tooltip("�л�����ʱ������λ��")]
        public Transform DownWeaponPosition;
        
        [Header("�����Ķ�̬�ڶ�")]
        [Tooltip("�����ƶ�ʱ�������ڶ�Ƶ��")]
        public float BobFrequency = 10f;
        [Tooltip("�����ƶ�ʱ�������ڶ�����")]
        public float BobSharpness = 10f;
        [Tooltip("δ��׼ʱ���ƶ��ڶ�����")]
        public float DefaultBobAmount = 0.05f;
        [Tooltip("��׼ʱ���ƶ��ڶ�����")]
        public float AimingBobAmount = 0.02f;
        [Tooltip("��׼ʱ���ӽ��л��ٶ�")]
        public float AimingAnimationSpeed = 10f;

        [Header("�����ĺ�����")]
        [Tooltip("��������������Ӧ�ٶ�")]
        public float RecoilSharpness = 50f;
        [Tooltip("������������������")]
        public float MaxRecoilDistance = 0.5f;
        [Tooltip("���س�ʼ��̬ʱ���ٶ�")]
        public float RecoilRestitutionSharpness = 10f;

        [Header("��ʾ����")]
        [Tooltip("����ͷ��Ĭ���Ӿ�")]
        public float DefaultFov = 60f;
        [Tooltip("��������ͷ��ʾ��ͼ��")]
        public LayerMask FpsWeaponLayer;

        public bool IsAiming { get; private set; }//�Ƿ�����׼״̬
        public bool IsPointingAtEnemy { get; private set; }//�Ƿ�����׼����
        public int ActiveWeaponIndex { get; private set; }//��ǰ����Index



        //����UI�������������Ķ�̬�л���Ч��
        public UnityAction<WeaponController> OnSwitchedToWeapon;
        public UnityAction<WeaponController, int> OnAddedWeapon;
        public UnityAction<WeaponController, int> OnRemovedWeapon;

        WeaponController[] m_WeaponSlots = new WeaponController[6];//��װ�ص�������
        PlayerInputHandler m_InputHandler;//���뵥Ԫ
        PlayerCharacterController m_PlayerCharacterController;

        Vector3 m_WeaponMainLocalPosition;//������λ��
        Vector3 m_AccumulatedRecoil;//���۵�����������
        Vector3 m_WeaponBobLocalPosition;//�����ĵ�ǰ֡����λ�ã����ڿ��ƻζ�
        Vector3 m_WeaponRecoilLocalPosition;//�����ĵ�ǰ֡�µĺ��ƾ��룬���ڿ��ƺ�����Ч��
        Vector3 m_LastCharacterPosition;//��������һ֡����λ�ã��ۺ��˻ζ��ͺ������ĵ���̬Ч��

        float m_WeaponBobFactor;//�����ζ�����

        WeaponSwitchState m_WeaponSwitchState;//��������״̬
        int m_WeaponSwitchNewWeaponIndex;//�����л������������
        float m_TimeStartedWeaponSwitch;//�����л��Ŀ�ʼʱ��
        


        // Start is called before the first frame update
        void Start()
        {
            ActiveWeaponIndex = -1;//��ʼΪ��������״̬
            IsAiming = false;
            m_InputHandler = GetComponent<PlayerInputHandler>();
            m_PlayerCharacterController = GetComponent<PlayerCharacterController>();

            m_WeaponSwitchState = WeaponSwitchState.Down;//�޳�ǹ
            SetFov(DefaultFov);

            OnSwitchedToWeapon += OnWeaponSwitched;

            foreach (var weapon in StartingWeapons)
            {
                AddWeapon(weapon);
            }
            //�л�����һ�����ص�����������������ǵ�һ����
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
                //���ӵ�
                if (m_InputHandler.GetReloadButtonDown() && activeWeapon.GetCurrentAmmoRatio() < 1.0f&& !activeWeapon.IsReloading)
                {
                    IsAiming = false;
                    activeWeapon.StartReloadAnimation();
                    return;
                }
                //������׼
                IsAiming = m_InputHandler.GetAimInputHeld();
                //�ж��Ƿ��ڿ�ǹת̨
                bool hasFired = activeWeapon.HandleShootInputs(
                    m_InputHandler.GetFireInputDown(),
                    m_InputHandler.GetFireInputHeld(),
                    m_InputHandler.GetFireInputReleased());

                if (hasFired)
                {
                    //�ڴ˴������ǹе���������λ��0313

                    m_AccumulatedRecoil += Vector3.back;
                    //��ֹ��������������
                    m_AccumulatedRecoil = Vector3.ClampMagnitude(m_AccumulatedRecoil, MaxRecoilDistance);
                }
            }
            //��ǹ
            if (!IsAiming &&(m_WeaponSwitchState == WeaponSwitchState.Up || m_WeaponSwitchState == WeaponSwitchState.Down))
            {
                int switchWeaponInput = m_InputHandler.GetSwitchWeaponInput();//�������
                if (switchWeaponInput != 0)
                {
                    bool switchUp = switchWeaponInput > 0;
                    SwitchWeapon(switchUp);
                }
                else
                {
                    switchWeaponInput = m_InputHandler.GetSelectWeaponInput();//���ּ������ѡ��
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
                        IsPointingAtEnemy = true;//����Ѱ��
                    }
                }
            }
        }
        //LateUpdate������Update��ɺ󱻵��ã�һ�����ڴ�����Ƶ��Ҫ��Ƚ��ȶ�������ϵͳ
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
        /// ���õ�ǰCamera��WeaponCamera
        /// </summary>
        /// <param name="fov">�Ӿ࣬Ĭ��60</param>
        public void SetFov(float fov)
        {
            //ͬ���л�
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
        /// ���һ���µ�����
        /// </summary>
        /// <param name="weaponPrefab">��ǰ������Ԥ���壬��Ҫ��WeaponController���</param>
        /// <returns>�Ƿ����</returns>
        public bool AddWeapon(WeaponController weaponPrefab)
        {
            if (HasWeapon(weaponPrefab))
                return false;
            //�滻��Ч��ǹ֧
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

                    // �Ը���������ָ����ʾͼ��
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
            //���ʧ��
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
        /// �����л���ǰ�����������β��ж����������
        /// </summary>
        /// <param name="ascendingOrder">true����/false����</param>
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

                //��ǰ��������̧��������
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
                // ���µ�ǰ���������л���ǰ����״̬
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
                distanceBetweenSlots = toSlotIndex - fromSlotIndex;//����
            else
                distanceBetweenSlots = -1 * (toSlotIndex - fromSlotIndex);//����
            if (distanceBetweenSlots < 0)
                distanceBetweenSlots = m_WeaponSlots.Length + distanceBetweenSlots;//����
            return distanceBetweenSlots;
        }
        //�����׼���ӽ��л�
        void UpdateWeaponAiming()
        {
            if (m_WeaponSwitchState == WeaponSwitchState.Up)
            {
                WeaponController activeWeapon = GetActiveWeapon();
                if (IsAiming && activeWeapon != null)
                {
                    //�л�������ָ��λ��
                    m_WeaponMainLocalPosition = Vector3.Lerp(m_WeaponMainLocalPosition,
                        AimingWeaponPosition.localPosition + activeWeapon.AimOffset,
                        AimingAnimationSpeed * Time.deltaTime);
                    //ͬ�������л�camera
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
        /// ���µ�ǰ���������Ž�ɫ�ƶ�ʱ��λ���л�
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

        //������ʱ�ĺ�����
        void UpdateWeaponRecoil()
        {
            if (m_WeaponRecoilLocalPosition.z >= m_AccumulatedRecoil.z * 0.99f)
            {
                //�ѵ����ֵ
                m_WeaponRecoilLocalPosition = Vector3.Lerp(m_WeaponRecoilLocalPosition, m_AccumulatedRecoil,
                    RecoilSharpness * Time.deltaTime);
            }
            else
            {
                //��ʼ���ػ���
                m_WeaponRecoilLocalPosition = Vector3.Lerp(m_WeaponRecoilLocalPosition, Vector3.zero,
                    RecoilRestitutionSharpness * Time.deltaTime);
                m_AccumulatedRecoil = m_WeaponRecoilLocalPosition;
            }
        }

        void UpdateWeaponSwitching()
        {
            float switchingTimeFactor = Mathf.Clamp01((Time.time - m_TimeStartedWeaponSwitch)/0.2f);//���ۼƵ���ǹʱ��
            if (switchingTimeFactor >= 1f)
            {
                if (m_WeaponSwitchState == WeaponSwitchState.PutDownPrevious)
                {
                    WeaponController oldWeapon = GetWeaponAtSlotIndex(ActiveWeaponIndex);
                    if (oldWeapon != null)
                    {
                        //����
                        oldWeapon.ShowWeapon(false);
                    }

                    ActiveWeaponIndex = m_WeaponSwitchNewWeaponIndex;
                    switchingTimeFactor = 0f;

                    WeaponController newWeapon = GetWeaponAtSlotIndex(ActiveWeaponIndex);
                    if (OnSwitchedToWeapon != null)
                    {
                        OnSwitchedToWeapon.Invoke(newWeapon);//UI����
                    }
                    if (newWeapon)
                    {
                        m_TimeStartedWeaponSwitch = Time.time;//����ˢ��ʱ�䣬׼��̧ǹ
                        m_WeaponSwitchState = WeaponSwitchState.PutUpNew;
                    }
                    else
                    {
                        // ��ǹе�ɽ��и���
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
