using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.FPS.Game;

namespace Unity.FPS.Player
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerInputHandler), typeof(AudioSource))]
    public class PlayerCharacterController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("�������")]
        public Camera PlayerCamera;

        [Tooltip("��Ч������")]
        public AudioSource AudioSource;

        [Header("General")]
        [Tooltip("�������½���")]
        public float GravityDownForce = 20f;
        [Tooltip("�ж����ڱ�ʾ�����Layer�ּ�")]
        public LayerMask GroundCheckLayers = -1;
        [Tooltip("���Ե���ľ���")]
        public float GroundCheckDistance = 0.05f;

        [Header("Move")]
        [Tooltip("�Ǳ���ʱ�ڵ��������ٶ�")]
        public float MaxSpeedOnGround = 10f;
        [Tooltip("����ʱ���ٶȱ���")]
        [Range(0, 1)]
        public float MaxSpeedCrouchedRatio = 0.5f;
        [Tooltip("�Ǳ���ʱ�ڿ��е�����ٶ�")]
        public float MaxSpeedInAir = 8f;
        [Tooltip("���м�������")]
        public float AccelerationSpeedInAir = 25f;
        [Tooltip("shift���ܱ���")]
        public float SprintSpeedModifier = 2f;
        [Tooltip("�ٶ�ת�����ƽ�����ɣ���ֵ�������ɵ��ٶ�")]
        public float MovementSharpnessOnGround = 15;
        [Tooltip("�����߶�")]
        public float KillHeight = -50f;

        [Header("Rotation")]
        [Tooltip("��ת�ٶ�")]
        public float RotationSpeed = 200f;
        [Range(0.1f, 1f)]
        [Tooltip("��׼ʱ�ı���")]
        public float AimingRotationMultiplier = 0.4f;

        [Header("Jump")]
        [Tooltip("����ʱʩ�ӵ���")]
        public float JumpForce = 9f;

        [Header("Stance")]
        [Tooltip("վ���߶�")]
        public float CapsuleHeightStanding = 1.8f;

        [Tooltip("���¸߶�")]
        public float CapsuleHeightCrouching = 0.9f;

        [Tooltip("վ������µ��л�")]
        public float CrouchingSharpness = 10f;

        [Header("Audio")]
        [Tooltip("���������ƶ���Ч���ƶ�����")]
        public float FootstepSfxFrequency = 1f;

        [Tooltip("�첽��ʱ���������ƶ���Ч���ƶ�����")]
        public float FootstepSfxFrequencyWhileSprinting = 1f;

        [Tooltip("�Ų���")]
        public AudioClip FootstepSfx;

        [Tooltip("������")]
        public AudioClip JumpSfx;
        [Tooltip("�����")]
        public AudioClip LandSfx;


        public Vector3 CharacterVelocity { get; set; }
        public bool IsGrounded { get; private set; }//�Ƿ��ڵ���
        public bool HasJumpedThisFrame { get; private set; }//�Ƿ�����
        public bool IsDead { get; private set; }//�Ƿ�������
        public bool IsCrouching { get; private set; }//�Ƿ����

        public float RotationMultiplier
        {
            get
            {
                if (m_WeaponsManager.IsAiming)
                {
                    return AimingRotationMultiplier;
                }

                return 1f;
            }
        }

        Health m_Health;//��������
        PlayerInputHandler m_InputHandler;//�������
        CharacterController m_Controller;//���������
        Actor m_Actor;
        PlayerWeaponsManager m_WeaponsManager;

        Vector3 m_GroundNormal;//��ǰ����ڵ���ķ�������
        float m_LastTimeJumped = 0f;//����֡��ʼ��ʱ��
        float m_FootstepDistanceCounter;//�������߶��ľ��룬������·�����Ƿ񲥷�

        float m_CameraVerticalAngle = 0f;//��ǰ��ͷ�ڴ�ֱ�����������ĽǶ�(������-89~89)
        float m_TargetCharacterHeight;//��ǰ����ĸ߶ȣ�վ�����£�
        Vector3 m_LatestImpactSpeed;//�����Ӱ����ٶȾ���

        const float k_JumpGroundingPreventionTime = 0.2f;
        const float k_GroundCheckDistanceInAir = 0.07f;

        // Start is called before the first frame update
        void Start()
        {
            ActorsManager actorsManager = FindObjectOfType<ActorsManager>();
            if (actorsManager != null)
                actorsManager.SetPlayer(gameObject);//�������
            m_Health = GetComponent<Health>();
            m_InputHandler = GetComponent<PlayerInputHandler>();
            m_Controller = GetComponent<CharacterController>();
            m_Actor = GetComponent<Actor>();
            m_WeaponsManager = GetComponent<PlayerWeaponsManager>();
            m_Health.OnDie += OnDie;//������������

            SetCrouchingState(false, true);//������ײ����վ��
            UpdateCharacterHeight(true);
        }

        // Update is called once per frame
        void Update()
        {
            if (!IsDead && transform.position.y < KillHeight)
            {
                m_Health.Kill();
            }

            HasJumpedThisFrame = false;
            bool wasGrounded = IsGrounded;
            GroundCheck();

            if(IsGrounded && !wasGrounded)
            {
                //���������
                AudioSource.PlayOneShot(LandSfx);
            }

            // crouching
            if (m_InputHandler.GetCrouchInputDown())
            {
                SetCrouchingState(!IsCrouching, false);
            }

            UpdateCharacterHeight(false);

            HandleCharacterMovement();
        }

        void OnDie()
        {
            IsDead = true;
            EventManager.Broadcast(Events.PlayerDeathEvent);
        }

        void GroundCheck()
        {
            float chosenGroundCheckDistance =
                IsGrounded ? (m_Controller.skinWidth + GroundCheckDistance) : k_GroundCheckDistanceInAir;

            // �����ж�
            IsGrounded = false;
            m_GroundNormal = Vector3.up;
            if (Time.time >= m_LastTimeJumped + k_JumpGroundingPreventionTime)
            {
                if (Physics.CapsuleCast(GetCapsuleBottomHemisphere(), GetCapsuleTopHemisphere(m_Controller.height),
                    m_Controller.radius, Vector3.down, out RaycastHit hit, chosenGroundCheckDistance, GroundCheckLayers,
                    QueryTriggerInteraction.Ignore))
                {
                    m_GroundNormal = hit.normal;
                    if (Vector3.Dot(hit.normal, transform.up) > 0f &&
                        IsNormalUnderSlopeLimit(m_GroundNormal))
                    {
                        IsGrounded = true;
                        if (hit.distance > m_Controller.skinWidth)
                        {
                            m_Controller.Move(Vector3.down * hit.distance);
                        }
                    }
                }
            }
        }
        // Returns true if the slope angle represented by the given normal is under the slope angle limit of the character controller
        bool IsNormalUnderSlopeLimit(Vector3 normal)
        {
            return Vector3.Angle(transform.up, normal) <= m_Controller.slopeLimit;
        }

        void HandleCharacterMovement()
        {
            //��ֱ������ת
            transform.Rotate(
                    new Vector3(0f, (m_InputHandler.GetLookInputsHorizontal() * RotationSpeed * RotationMultiplier),
                        0f), Space.Self);
            //ƽ�Ʒ�����ת����Ҫ�޶��ڣ�-89~89��
            m_CameraVerticalAngle += m_InputHandler.GetLookInputsVertical() * RotationSpeed * RotationMultiplier;
            m_CameraVerticalAngle = Mathf.Clamp(m_CameraVerticalAngle, -89f, 89f);
            PlayerCamera.transform.localEulerAngles = new Vector3(m_CameraVerticalAngle, 0, 0);//���õ�ǰ�Ƕ�

            //����ʱ�޷����ߣ���Ҫ��������վ�����Ҳ��ú����ϲ���ײ��
            bool isSprinting = m_InputHandler.GetSprintInputHeld();//����shift
            if (isSprinting)
            {
                isSprinting = SetCrouchingState(false, false);
            }
            float speedModifier = isSprinting ? SprintSpeedModifier : 1f;
            Vector3 worldspaceMoveInput = transform.TransformVector(m_InputHandler.GetMoveInput());
            //�ڵ���
            if (IsGrounded)
            {
                Vector3 targetVelocity = worldspaceMoveInput * MaxSpeedOnGround * speedModifier;
                //����ʱ�ٶȱ���
                if (IsCrouching)
                    targetVelocity *= MaxSpeedCrouchedRatio;
                //����б������µķ�������
                targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, m_GroundNormal) *
                                     targetVelocity.magnitude;

                CharacterVelocity = Vector3.Lerp(CharacterVelocity, targetVelocity,
                        MovementSharpnessOnGround * Time.deltaTime);

                //׼������
                if (IsGrounded && m_InputHandler.GetJumpInputDown())
                {
                    //��������ײ
                    if (SetCrouchingState(false, false))
                    {
                        CharacterVelocity = new Vector3(CharacterVelocity.x, 0f, CharacterVelocity.z);
                        CharacterVelocity += Vector3.up * JumpForce;
                        AudioSource.PlayOneShot(JumpSfx);
                        m_LastTimeJumped = Time.time;
                        HasJumpedThisFrame = true;
                        IsGrounded = false;
                        m_GroundNormal = Vector3.up;
                    }
                }
                //���ܺ�����ʱ��Ƶ��
                float chosenFootstepSfxFrequency =
                        (isSprinting ? FootstepSfxFrequencyWhileSprinting : FootstepSfxFrequency);
                if (m_FootstepDistanceCounter >= 1f / chosenFootstepSfxFrequency)
                {
                    m_FootstepDistanceCounter = 0f;
                    AudioSource.PlayOneShot(FootstepSfx);
                }
                m_FootstepDistanceCounter += CharacterVelocity.magnitude * Time.deltaTime;
            }
            //�ڿ���
            else 
            {
                // ���м��ٶ�
                CharacterVelocity += worldspaceMoveInput * AccelerationSpeedInAir * Time.deltaTime;

                // �����ٶ�
                float verticalVelocity = CharacterVelocity.y;
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(CharacterVelocity, Vector3.up);
                horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, MaxSpeedInAir * speedModifier);
                CharacterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);

                // �½���
                CharacterVelocity += Vector3.down * GravityDownForce * Time.deltaTime;
            }
            //�����ƶ�
            Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
            Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(m_Controller.height);
            m_Controller.Move(CharacterVelocity * Time.deltaTime);

            //�ָ��ƶ�
            m_LatestImpactSpeed = Vector3.zero;
            if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, m_Controller.radius,
                CharacterVelocity.normalized, out RaycastHit hit, CharacterVelocity.magnitude * Time.deltaTime, -1,
                QueryTriggerInteraction.Ignore))
            {
                //��һ֡���ٶ�
                m_LatestImpactSpeed = CharacterVelocity;

                CharacterVelocity = Vector3.ProjectOnPlane(CharacterVelocity, hit.normal);
            }
        }

        //�����������ã�������б��ʱ��������ƫ�Ƶ�����
        public Vector3 GetDirectionReorientedOnSlope(Vector3 direction, Vector3 slopeNormal)
        {
            Vector3 directionRight = Vector3.Cross(direction, transform.up);
            return Vector3.Cross(slopeNormal, directionRight).normalized;
        }

        bool SetCrouchingState(bool crouched, bool ignoreObstructions)
        {
            // set appropriate heights
            if (crouched)
            {
                m_TargetCharacterHeight = CapsuleHeightCrouching;
            }
            else
            {
                // �л�Ϊվ��ʱ������ϲ�����ײ�����޷�վ��
                if (!ignoreObstructions)
                {
                    // ժҪ:
                    //     Check the given capsule against the physics world and return all overlapping
                    //     colliders.
                    // ����:
                    //   point0:The center of the sphere at the start of the capsule.
                    //   point1:The center of the sphere at the end of the capsule.
                    //   radius:The radius of the capsule.
                    //   layerMask:A that is used to selectively ignore colliders when casting a capsule.
                    //   queryTriggerInteraction:Specifies whether this query should hit Triggers.
                    // ���ؽ��:
                    //     Colliders touching or inside the capsule.
                    //ͨ����������ɽ������ж���ײ
                    Collider[] standingOverlaps = Physics.OverlapCapsule(
                        GetCapsuleBottomHemisphere(),
                        GetCapsuleTopHemisphere(CapsuleHeightStanding),
                        m_Controller.radius,
                        -1,
                        QueryTriggerInteraction.Ignore);
                    foreach (Collider c in standingOverlaps)
                    {
                        //��������ײ�Ҳ�Ϊ����ʱ
                        if (c != m_Controller)
                        {
                            return false;//������ײ
                        }
                    }
                }
                //վ�𣬲����õ�ǰ�߶�
                m_TargetCharacterHeight = CapsuleHeightStanding;
            }
            IsCrouching = crouched;
            return true;
        }

        //��õ�ǰ������ĵײ�����
        Vector3 GetCapsuleBottomHemisphere()
        {
            return transform.position + (transform.up * m_Controller.radius);
        }
        //��õ�ǰ������Ķ�������
        Vector3 GetCapsuleTopHemisphere(float atHeight)
        {
            return transform.position + (transform.up * (atHeight - m_Controller.radius));
        }

        void UpdateCharacterHeight(bool force)
        {
            //�����佺��������
            if (force)
            {
                m_Controller.height = m_TargetCharacterHeight;
                m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
                PlayerCamera.transform.localPosition = Vector3.up * m_TargetCharacterHeight * 0.9f;
                m_Actor.AimPoint.transform.localPosition = m_Controller.center;
            }
            else if (m_Controller.height != m_TargetCharacterHeight)
            {
                m_Controller.height = Mathf.Lerp(m_Controller.height, m_TargetCharacterHeight,
                    CrouchingSharpness * Time.deltaTime);
                m_Controller.center = Vector3.up * m_Controller.height * 0.5f;
                PlayerCamera.transform.localPosition = Vector3.Lerp(PlayerCamera.transform.localPosition,
                    Vector3.up * m_TargetCharacterHeight * 0.9f, CrouchingSharpness * Time.deltaTime);
                m_Actor.AimPoint.transform.localPosition = m_Controller.center;
            }
        }
    }
}
