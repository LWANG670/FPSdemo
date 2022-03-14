using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Weapon1 : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform firePoint;
    public GameObject firePre;//����Ԥ����
    public Transform bulletPoint;
    public GameObject bulletPre;//�ӵ�Ԥ����

    public int bulletCount = 30;
    [SerializeField]
    private AudioClip shotAudio;
    public float cd = 0.2f;

    private int curBulletCount;
    private float timer = 0;

    private float hurt = 1.0f;
    void Start()
    {
        curBulletCount = bulletCount;
    }

    // Update is called once per frame
    void Update()
    {
        //bulletCountText.text = "�ӵ�����" + curBulletCount;
        timer += Time.deltaTime;
        if (timer > cd && Input.GetMouseButton(0)&&curBulletCount > 0)
        {
            //��ʼ��������ӵ�
            Instantiate(firePre, firePoint.position, firePoint.rotation);
            Instantiate(bulletPre, bulletPoint.position, bulletPoint.rotation);

            curBulletCount--;
            GetComponent<AudioSource>().PlayOneShot(shotAudio);
            if (curBulletCount == 0)
            {
                GetComponent<Animator>().SetTrigger("Reload");
                //����
                Invoke("reload", 1.5f);
            }
            timer = 0;
        }
        if ((Input.GetKeyDown(KeyCode.R) && curBulletCount < bulletCount))
        {
            if(GetComponent<Animator>()!=null)
                GetComponent<Animator>().SetTrigger("Reload");
            //����
            Invoke("reload", 1.5f);
        }
    }
    /// <summary>
    /// ������װ�ӵ�
    /// </summary>
    public void reload()
    {
         curBulletCount = bulletCount;
    }
}
