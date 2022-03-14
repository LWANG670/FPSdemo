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
        [Tooltip("主摄像机")]
        public Camera PlayerCamera;

        [Tooltip("音效播放器")]
        public AudioSource AudioSource;

        [Header("General")]
        [Tooltip("跳起后的下降力")]
        public float GravityDownForce = 20f;
        [Tooltip("判断用于表示地面的Layer分级")]
        public LayerMask GroundCheckLayers = -1;
        [Tooltip("测试地面的距离")]
        public float GroundCheckDistance = 0.05f;

        [Header("Move")]
        [Tooltip("非奔跑时在地面的最快速度")]
        public float MaxSpeedOnGround = 10f;
        [Tooltip("蹲下时的速度倍率")]
        [Range(0, 1)]
        public float MaxSpeedCrouchedRatio = 0.5f;
        [Tooltip("非奔跑时在空中的最快速度")]
        public float MaxSpeedInAir = 8f;
        [Tooltip("空中加速速率")]
        public float AccelerationSpeedInAir = 25f;
        [Tooltip("shift快跑倍率")]
        public float SprintSpeedModifier = 2f;
        [Tooltip("速度转换点的平滑过渡，数值决定过渡的速度")]
        public float MovementSharpnessOnGround = 15;
        [Tooltip("死亡高度")]
        public float KillHeight = -50f;

        [Header("Rotation")]
        [Tooltip("旋转速度")]
        public float RotationSpeed = 200f;
        [Range(0.1f, 1f)]
        [Tooltip("瞄准时的倍率")]
        public float AimingRotationMultiplier = 0.4f;

        [Header("Jump")]
        [Tooltip("跳起时施加的力")]
        public float JumpForce = 9f;

        [Header("Stance")]
        [Tooltip("站立高度")]
        public float CapsuleHeightStanding = 1.8f;

        [Tooltip("蹲下高度")]
        public float CapsuleHeightCrouching = 0.9f;

        [Tooltip("站起与蹲下的切换")]
        public float CrouchingSharpness = 10f;

        [Header("Audio")]
        [Tooltip("决定播放移动音效的移动距离")]
        public float FootstepSfxFrequency = 1f;

        [Tooltip("快步走时决定播放移动音效的移动距离")]
        public float FootstepSfxFrequencyWhileSprinting = 1f;

        [Tooltip("脚步声")]
        public AudioClip FootstepSfx;

        [Tooltip("跳起声")]
        public AudioClip JumpSfx;
        [Tooltip("落地声")]
        public AudioClip LandSfx;


        public Vector3 CharacterVelocity { get; set; }
        public bool IsGrounded { get; private set; }//是否在地面
        public bool HasJumpedThisFrame { get; private set; }//是否跳起
        public bool IsDead { get; private set; }//是否已死亡
        public bool IsCrouching { get; private set; }//是否蹲下

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

        Health m_Health;//生命监视
        PlayerInputHandler m_InputHandler;//输入监视
        CharacterController m_Controller;//胶囊体监视
        Actor m_Actor;
        PlayerWeaponsManager m_WeaponsManager;

        Vector3 m_GroundNormal;//当前相对于地面的方向向量
        float m_LastTimeJumped = 0f;//起跳帧开始的时间
        float m_FootstepDistanceCounter;//根据以走动的距离，决定走路声音是否播放

        float m_CameraVerticalAngle = 0f;//当前镜头在垂直方向上所处的角度(限制在-89~89)
        float m_TargetCharacterHeight;//当前对象的高度（站起或蹲下）
        Vector3 m_LatestImpactSpeed;//最后受影响的速度距离

        const float k_JumpGroundingPreventionTime = 0.2f;
        const float k_GroundCheckDistanceInAir = 0.07f;

        // Start is called before the first frame update
        void Start()
        {
            ActorsManager actorsManager = FindObjectOfType<ActorsManager>();
            if (actorsManager != null)
                actorsManager.SetPlayer(gameObject);//设置玩家
            m_Health = GetComponent<Health>();
            m_InputHandler = GetComponent<PlayerInputHandler>();
            m_Controller = GetComponent<CharacterController>();
            m_Actor = GetComponent<Actor>();
            m_WeaponsManager = GetComponent<PlayerWeaponsManager>();
            m_Health.OnDie += OnDie;//任务死亡函数

            SetCrouchingState(false, true);//无视碰撞，且站起
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
                //播放落地声
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

            // 重置判断
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
            //垂直方向旋转
            transform.Rotate(
                    new Vector3(0f, (m_InputHandler.GetLookInputsHorizontal() * RotationSpeed * RotationMultiplier),
                        0f), Space.Self);
            //平移方向旋转，需要限定在（-89~89）
            m_CameraVerticalAngle += m_InputHandler.GetLookInputsVertical() * RotationSpeed * RotationMultiplier;
            m_CameraVerticalAngle = Mathf.Clamp(m_CameraVerticalAngle, -89f, 89f);
            PlayerCamera.transform.localEulerAngles = new Vector3(m_CameraVerticalAngle, 0, 0);//设置当前角度

            //蹲下时无法快走，需要先设置其站立，且不得忽视上侧碰撞物
            bool isSprinting = m_InputHandler.GetSprintInputHeld();//快走shift
            if (isSprinting)
            {
                isSprinting = SetCrouchingState(false, false);
            }
            float speedModifier = isSprinting ? SprintSpeedModifier : 1f;
            Vector3 worldspaceMoveInput = transform.TransformVector(m_InputHandler.GetMoveInput());
            //在地面
            if (IsGrounded)
            {
                Vector3 targetVelocity = worldspaceMoveInput * MaxSpeedOnGround * speedModifier;
                //蹲下时速度倍率
                if (IsCrouching)
                    targetVelocity *= MaxSpeedCrouchedRatio;
                //计算斜坡情况下的方向向量
                targetVelocity = GetDirectionReorientedOnSlope(targetVelocity.normalized, m_GroundNormal) *
                                     targetVelocity.magnitude;

                CharacterVelocity = Vector3.Lerp(CharacterVelocity, targetVelocity,
                        MovementSharpnessOnGround * Time.deltaTime);

                //准备起跳
                if (IsGrounded && m_InputHandler.GetJumpInputDown())
                {
                    //不发生碰撞
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
                //快跑和慢跑时的频率
                float chosenFootstepSfxFrequency =
                        (isSprinting ? FootstepSfxFrequencyWhileSprinting : FootstepSfxFrequency);
                if (m_FootstepDistanceCounter >= 1f / chosenFootstepSfxFrequency)
                {
                    m_FootstepDistanceCounter = 0f;
                    AudioSource.PlayOneShot(FootstepSfx);
                }
                m_FootstepDistanceCounter += CharacterVelocity.magnitude * Time.deltaTime;
            }
            //在空中
            else 
            {
                // 空中加速度
                CharacterVelocity += worldspaceMoveInput * AccelerationSpeedInAir * Time.deltaTime;

                // 限制速度
                float verticalVelocity = CharacterVelocity.y;
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(CharacterVelocity, Vector3.up);
                horizontalVelocity = Vector3.ClampMagnitude(horizontalVelocity, MaxSpeedInAir * speedModifier);
                CharacterVelocity = horizontalVelocity + (Vector3.up * verticalVelocity);

                // 下降力
                CharacterVelocity += Vector3.down * GravityDownForce * Time.deltaTime;
            }
            //计算移动
            Vector3 capsuleBottomBeforeMove = GetCapsuleBottomHemisphere();
            Vector3 capsuleTopBeforeMove = GetCapsuleTopHemisphere(m_Controller.height);
            m_Controller.Move(CharacterVelocity * Time.deltaTime);

            //恢复移动
            m_LatestImpactSpeed = Vector3.zero;
            if (Physics.CapsuleCast(capsuleBottomBeforeMove, capsuleTopBeforeMove, m_Controller.radius,
                CharacterVelocity.normalized, out RaycastHit hit, CharacterVelocity.magnitude * Time.deltaTime, -1,
                QueryTriggerInteraction.Ignore))
            {
                //上一帧的速度
                m_LatestImpactSpeed = CharacterVelocity;

                CharacterVelocity = Vector3.ProjectOnPlane(CharacterVelocity, hit.normal);
            }
        }

        //方向有输入获得，在限制斜坡时，计算其偏移的向量
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
                // 切换为站起时，如果上侧有碰撞，则无法站起
                if (!ignoreObstructions)
                {
                    // 摘要:
                    //     Check the given capsule against the physics world and return all overlapping
                    //     colliders.
                    // 参数:
                    //   point0:The center of the sphere at the start of the capsule.
                    //   point1:The center of the sphere at the end of the capsule.
                    //   radius:The radius of the capsule.
                    //   layerMask:A that is used to selectively ignore colliders when casting a capsule.
                    //   queryTriggerInteraction:Specifies whether this query should hit Triggers.
                    // 返回结果:
                    //     Colliders touching or inside the capsule.
                    //通过两个球组成胶囊体判断碰撞
                    Collider[] standingOverlaps = Physics.OverlapCapsule(
                        GetCapsuleBottomHemisphere(),
                        GetCapsuleTopHemisphere(CapsuleHeightStanding),
                        m_Controller.radius,
                        -1,
                        QueryTriggerInteraction.Ignore);
                    foreach (Collider c in standingOverlaps)
                    {
                        //当发生碰撞且不为自身时
                        if (c != m_Controller)
                        {
                            return false;//发生碰撞
                        }
                    }
                }
                //站起，并设置当前高度
                m_TargetCharacterHeight = CapsuleHeightStanding;
            }
            IsCrouching = crouched;
            return true;
        }

        //获得当前胶囊体的底部球心
        Vector3 GetCapsuleBottomHemisphere()
        {
            return transform.position + (transform.up * m_Controller.radius);
        }
        //获得当前胶囊体的顶部球心
        Vector3 GetCapsuleTopHemisphere(float atHeight)
        {
            return transform.position + (transform.up * (atHeight - m_Controller.radius));
        }

        void UpdateCharacterHeight(bool force)
        {
            //设置其胶囊体属性
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
