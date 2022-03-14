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
    [Tooltip("当前武器类型")]
    public FPS.WeaponType weaponType = FPS.WeaponType.Projectile;
    [Tooltip("发射模式自动或者半自动")]
    public AutoFireType autoFireType = AutoFireType.Full;
    [Tooltip("发射效果位置")]
    public Transform firePoint;
    [Tooltip("发射效果预制体集合,可能有多个效果叠加")]
    public List<GameObject> firePres;
    [Tooltip("子弹发射位置")]
    public Transform bulletPoint;
    [Tooltip("子弹预制体")]
    public GameObject bulletPre;//子弹预设体
    [Tooltip("是否显示当前子弹数")]
    public bool showCurrentBullet = true;
    [Tooltip("子弹弹药数")]
    public int bulletCount = 30;
    [Tooltip("子弹弹药发射CD")]
    public float firePause = 0.2f;
    [Tooltip("子弹装填时间")]
    public float reloadTime = 1.5f;

    public Image CurAmmImage;

    //各声音设置
    [SerializeField]
    [Tooltip("子弹发射音效")]
    private AudioClip shotAudio;
    [Tooltip("装填音效")]
    public AudioClip reloadAudio;

    private int curBulletCount;
    private float timer = 0;

    Recoil recoil;


    void Start()
    {
        curBulletCount = bulletCount;
        //获得当前武器的后座力设置
        recoil = GetComponent<Recoil>();
    }

    // Update is called once per frame
    void Update()
    {
        //bulletCountText.text = "子弹数：" + curBulletCount;
        timer += Time.deltaTime;
        if (timer > firePause && Input.GetMouseButton(0) && curBulletCount > 0)
        {
            //初始化火焰和子弹
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
                //反射
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
            //反射
            Invoke("reload", reloadTime);
        }
    }
    /// <summary>
    /// 重新填装子弹
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
            //    GUI.Label(new Rect(10, Screen.height - 30, 100, 20), "子弹数: " + curBulletCount);
            //else if (weaponType == FPS.WeaponType.Beam)
            //    GUI.Label(new Rect(10, Screen.height - 30, 100, 20), "激光能量: " + (int)(curBulletCount * 10) + "/" + (int)(bulletCount * 10));
        }
    }
}
