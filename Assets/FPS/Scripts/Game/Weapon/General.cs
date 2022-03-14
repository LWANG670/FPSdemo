using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum AutoFireType
{
    Full,Semi
}
namespace FPS
{
    public enum WeaponType
    {
        Projectile,
        Raycast,
        Beam
    }
}


public class General : MonoBehaviour
{
    [Tooltip("��ǰ��������")]
    public FPS.WeaponType weaponType = FPS.WeaponType.Projectile;
    [Tooltip("����ģʽ�Զ����߰��Զ�")]
    public AutoFireType autoFireType = AutoFireType.Full;
    [Tooltip("����Ч��λ��")]
    public Transform firePoint;
    [Tooltip("����Ч��Ԥ���弯��,�����ж��Ч������")]
    public List<GameObject> firePres;
    [Tooltip("�ӵ�����λ��")]
    public Transform bulletPoint;
    [Tooltip("�ӵ�Ԥ����")]
    public GameObject bulletPre;//�ӵ�Ԥ����
    [Tooltip("�Ƿ���ʾ��ǰ�ӵ���")]
    public bool showCurrentBullet = true;
    [Tooltip("�ӵ���ҩ��")]
    public int bulletCount = 30;
    [Tooltip("�ӵ���ҩ����CD")]
    public float firePause = 0.2f;
    [Tooltip("�ӵ�װ��ʱ��")]
    public float reloadTime = 1.5f;

    public Image CurAmmImage;

    //����������
    [SerializeField]
    [Tooltip("�ӵ�������Ч")]
    private AudioClip shotAudio;
    [Tooltip("װ����Ч")]
    public AudioClip reloadAudio;

    private int curBulletCount;
    private float timer = 0;

    Recoil recoil;


    void Start()
    {
        curBulletCount = bulletCount;
        //��õ�ǰ�����ĺ���������
        recoil = GetComponent<Recoil>();
    }

    // Update is called once per frame
    void Update()
    {
        //bulletCountText.text = "�ӵ�����" + curBulletCount;
        timer += Time.deltaTime;
        if (timer > firePause && Input.GetMouseButton(0) && curBulletCount > 0)
        {
            //��ʼ��������ӵ�
            foreach(var firePre in firePres)
                Instantiate(firePre, firePoint.position, firePoint.rotation);
            Instantiate(bulletPre, bulletPoint.position, bulletPoint.rotation);
            curBulletCount--;
            if (recoil != null)
            {
                recoil.DoRecoil();
            }
            GetComponent<AudioSource>().PlayOneShot(shotAudio);
            if (curBulletCount == 0)
            {
                if (GetComponent<Animator>() != null)
                    GetComponent<Animator>().SetTrigger("Reload");
                if (reloadAudio != null)
                    GetComponent<AudioSource>().PlayOneShot(reloadAudio);
                //����
                Invoke("reload", reloadTime);
            }
            timer = 0;
        }
        if ((Input.GetKeyDown(KeyCode.R) && curBulletCount < bulletCount))
        {
            if (GetComponent<Animator>() != null)
                GetComponent<Animator>().SetTrigger("Reload");
            if (reloadAudio != null)
                GetComponent<AudioSource>().PlayOneShot(reloadAudio);
            //����
            Invoke("reload", reloadTime);
        }
    }
    /// <summary>
    /// ������װ�ӵ�
    /// </summary>
    public void reload()
    {
        curBulletCount = bulletCount;
    }
    // Start is called before the first frame update

    private void OnGUI()
    {
        if (showCurrentBullet&& CurAmmImage)
        {
            CurAmmImage.fillAmount = (float)curBulletCount / bulletCount;
            //if (weaponType == FPS.WeaponType.Raycast || weaponType == FPS.WeaponType.Projectile)
            //    GUI.Label(new Rect(10, Screen.height - 30, 100, 20), "�ӵ���: " + curBulletCount);
            //else if (weaponType == FPS.WeaponType.Beam)
            //    GUI.Label(new Rect(10, Screen.height - 30, 100, 20), "��������: " + (int)(curBulletCount * 10) + "/" + (int)(bulletCount * 10));
        }
    }
}
