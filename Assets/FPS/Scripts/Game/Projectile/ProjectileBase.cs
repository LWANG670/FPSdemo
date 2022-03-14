using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Gameplay
{
    public abstract class ProjectileBase : MonoBehaviour
    {
        public GameObject Owner { get; private set; }//子弹发出者
        public Vector3 InitialPosition { get; private set; }//子弹发出的位置
        public Vector3 InitialDirection { get; private set; }//子弹发出的方向


        public UnityAction OnShoot;//射击时刻的委托效果

        public void Shoot(WeaponController controller)
        {
            Owner = controller.Owner;//当前的拥有者
            InitialPosition = transform.position;
            InitialDirection = transform.forward;

            OnShoot?.Invoke();//发射效果
        }
    }
}
