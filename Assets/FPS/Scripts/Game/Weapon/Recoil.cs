using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Recoil : MonoBehaviour
{
    [Tooltip("�ӵ�����������")]
    public bool recoil = true;
    [Tooltip("���˾�����Сֵ")]
    public float recoilKickBackMin = 0.1f;
    [Tooltip("���˾������ֵ")]
    public float recoilKickBackMax = 0.3f;
    [Tooltip("����Ƕ���Сֵ")]
    public float recoilRotationMin = 0.1f;
    [Tooltip("����Ƕ����ֵ")]
    public float recoilRotationMax = 0.25f;
    [Tooltip("�ָ��ٶ�")]
    public float recoilRecoveryRate = 0.01f;
    [Tooltip("��ǰ������ģ�Ͳ���")]
    public GameObject weaponModel;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //�ָ�λ��
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
