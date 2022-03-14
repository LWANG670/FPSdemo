using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BulletType
{
    Standard,
    Seeker
}

namespace FPS
{
    public enum DamageType
    {
        Direct,
        Explosion
    }
}


public class Bullet : MonoBehaviour
{
    [Tooltip("�ӵ�����:ֱ�߻��߸���")]
    public BulletType bulletType = BulletType.Standard;
    [Tooltip("�ӵ��˺�����:ֱ�߻��߱�ը�˺�")]
    public FPS.DamageType damageType = FPS.DamageType.Direct;
    [Tooltip("�ӵ��˺�")]
    public float damage=1.0f;
    [Tooltip("�ӵ��ٶ�")]
    public float speed = 10.0f;
    [Tooltip("�ӵ�������")]
    public float initialForce = 1000.0f;
    [Tooltip("�ӵ�����ʱ��")]
    public float lifeTime = 15.0f;
    [Tooltip("Ѱ���ٶ�")]
    public float seekRate = 1.0f;
    [Tooltip("Ѱ�б�ǩ��")]
    public string seekTag = "Enemy";
    [Tooltip("Ѱ������ʱ����")]
    public float targetListUpdateRate = 1.0f;
    [Tooltip("��ըЧ��")]
    public GameObject explosion;
    [Tooltip("��ը��")]
    public float explosionForce = 5.0f;
    [Tooltip("��ը�뾶")]
    public float explosionRadius = 10.0f;

    private float lifeTimer = 0.0f;//�ӵ��������ʱ��
    private float targetListUpdateTimer = 0.0f;//��Ѱ��ʱ��
    private GameObject[] enemyList;//���˼���
    private CrossHair crossHair;
    // Start is called before the first frame update
    void Start()
    {
        GetComponent<Rigidbody>().AddRelativeForce(transform.forward * initialForce);
        enemyList = GameObject.FindGameObjectsWithTag(seekTag);

        if(initialForce==0)
            GetComponent<Rigidbody>().velocity = transform.forward * speed;

        crossHair= GameObject.FindObjectOfType<CrossHair>();
    }

    // Update is called once per frame
    void Update()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer > lifeTime)
            Destroy(gameObject);
        //���ٵ���
        if (bulletType == BulletType.Seeker)
        {
            targetListUpdateTimer+= Time.deltaTime;
            if (targetListUpdateTimer >= targetListUpdateRate)
            {
                enemyList = GameObject.FindGameObjectsWithTag(seekTag);
                targetListUpdateTimer = 0.0f;
                if (enemyList != null)
                {
                    //Ѱ�ҵ����ˣ�ȷ��������ˣ�����λ�ø���
                    float bestEnemyDis = -1.0f;
                    Vector3 target = transform.forward * 1000;//��ʼ����
                    foreach (var enemy in enemyList)
                    {
                        if (enemy != null)
                        {
                            Vector3 direction = enemy.transform.position - transform.position;
                            float dot = Vector3.Dot(direction.normalized, transform.forward);//���
                            if (dot > bestEnemyDis)
                            {
                                target = enemy.transform.position;
                                bestEnemyDis = dot;
                            }
                        }
                    }
                    Quaternion targetRotation = Quaternion.LookRotation(target - transform.position);//��������
                    transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * seekRate);
                }
            }
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        Hit(collision);
        Destroy(gameObject);
    }


    private void Hit(Collision collision)
    {
        
        //��������Ч��
        if (explosion != null)
        {
            Instantiate(explosion, collision.GetContact(0).point, Quaternion.identity);
        }
        if (damageType == FPS.DamageType.Direct)
        {
            if (collision.collider.tag == "Enemy") 
            {
                crossHair.onHit();
                //collision.collider.GetComponent<Collider>().GetComponent<Health>().TakeDamage(-damage, gameObject);
                collision.collider.GetComponent<Collider>().gameObject.SendMessageUpwards("ChangeHealth", -damage, SendMessageOptions.DontRequireReceiver);
                //���ٵ�ǰ���������ֵ
            }

        }
        else if (damageType == FPS.DamageType.Explosion)
        {
            //��õ�ǰ��������������
            Collider[] cols = Physics.OverlapSphere(transform.position, explosionRadius);
            //Ϊ������ɫʩ����
            List<Rigidbody> rigidbodies = new List<Rigidbody>();
            foreach (Collider col in cols)
            {
                //��Χ�˺�����
                float damageAmount = damage * (1 / Vector3.Distance(transform.position, col.transform.position));
                //collision.collider.GetComponent<Collider>().GetComponent<Health>().TakeDamage(-damage, gameObject);
                col.GetComponent<Collider>().gameObject.SendMessageUpwards("ChangeHealth", -damageAmount, SendMessageOptions.DontRequireReceiver);
                //������ը��
                if (col.attachedRigidbody != null) 
                {
                    var curForce = explosionForce * (1 / Vector3.Distance(transform.position, col.transform.position));
                    col.attachedRigidbody.AddExplosionForce(curForce, transform.position, explosionRadius, 1, ForceMode.Impulse);
                } 
            }
            //
        }
    }
}
