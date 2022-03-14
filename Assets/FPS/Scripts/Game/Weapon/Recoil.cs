using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Recoil : MonoBehaviour
{
    [Tooltip("子弹后座力设置")]
    public bool recoil = true;
    [Tooltip("后退距离最小值")]
    public float recoilKickBackMin = 0.1f;
    [Tooltip("后退距离最大值")]
    public float recoilKickBackMax = 0.3f;
    [Tooltip("弹起角度最小值")]
    public float recoilRotationMin = 0.1f;
    [Tooltip("弹起角度最大值")]
    public float recoilRotationMax = 0.25f;
    [Tooltip("恢复速度")]
    public float recoilRecoveryRate = 0.01f;
    [Tooltip("当前武器的模型参数")]
    public GameObject weaponModel;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //恢复位置
        if (recoil)
        {
            weaponModel.transform.position = Vector3.Lerp(weaponModel.transform.position, transform.position, recoilRecoveryRate * Time.deltaTime);
            weaponModel.transform.rotation = Quaternion.Lerp(weaponModel.transform.rotation, transform.rotation, recoilRecoveryRate * Time.deltaTime);
        }
    }

    public void DoRecoil()
    {
        if (weaponModel == null)
        {
            Debug.Log("weapon is null");
            return;
        }
        // Calculate random values for the recoil position and rotation
        float kickBack = Random.Range(recoilKickBackMin, recoilKickBackMax);
        float kickRot = Random.Range(recoilRotationMin, recoilRotationMax);

        // Apply the random values to the weapon's postion and rotation
        weaponModel.transform.Translate(new Vector3(0, 0, -kickBack), Space.Self);
        weaponModel.transform.Rotate(new Vector3(-kickRot, 0, 0), Space.Self);
    }
}
