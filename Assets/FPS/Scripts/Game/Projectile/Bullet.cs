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
    [Tooltip("子弹类型:直线或者跟踪")]
    public BulletType bulletType = BulletType.Standard;
    [Tooltip("子弹伤害类型:直线或者爆炸伤害")]
    public FPS.DamageType damageType = FPS.DamageType.Direct;
    [Tooltip("子弹伤害")]
    public float damage=1.0f;
    [Tooltip("子弹速度")]
    public float speed = 10.0f;
    [Tooltip("子弹刚体力")]
    public float initialForce = 1000.0f;
    [Tooltip("子弹存在时间")]
    public float lifeTime = 15.0f;
    [Tooltip("寻敌速度")]
    public float seekRate = 1.0f;
    [Tooltip("寻敌标签名")]
    public string seekTag = "Enemy";
    [Tooltip("寻敌搜索时间间隔")]
    public float targetListUpdateRate = 1.0f;
    [Tooltip("爆炸效果")]
    public GameObject explosion;
    [Tooltip("爆炸力")]
    public float explosionForce = 5.0f;
    [Tooltip("爆炸半径")]
    public float explosionRadius = 10.0f;

    private float lifeTimer = 0.0f;//子弹已生存的时间
    private float targetListUpdateTimer = 0.0f;//已寻敌时间
    private GameObject[] enemyList;//敌人集合
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
        //跟踪导弹
        if (bulletType == BulletType.Seeker)
        {
            targetListUpdateTimer+= Time.deltaTime;
            if (targetListUpdateTimer >= targetListUpdateRate)
            {
                enemyList = GameObject.FindGameObjectsWithTag(seekTag);
                targetListUpdateTimer = 0.0f;
                if (enemyList != null)
                {
                    //寻找到敌人，确定最近敌人，进行位置跟踪
                    float bestEnemyDis = -1.0f;
                    Vector3 target = transform.forward * 1000;//初始方向
                    foreach (var enemy in enemyList)
                    {
                        if (enemy != null)
                        {
                            Vector3 direction = enemy.transform.position - transform.position;
                            float dot = Vector3.Dot(direction.normalized, transform.forward);//点积
                            if (dot > bestEnemyDis)
                            {
                                target = enemy.transform.position;
                                bestEnemyDis = dot;
                            }
                        }
                    }
                    Quaternion targetRotation = Quaternion.LookRotation(target - transform.position);//调整朝向
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
        
        //产生击到效果
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
                //减少当前对象的生命值
            }

        }
        else if (damageType == FPS.DamageType.Explosion)
        {
            //获得当前附近的所有物体
            Collider[] cols = Physics.OverlapSphere(transform.position, explosionRadius);
            //为附近角色施加力
            List<Rigidbody> rigidbodies = new List<Rigidbody>();
            foreach (Collider col in cols)
            {
                //范围伤害减免
                float damageAmount = damage * (1 / Vector3.Distance(transform.position, col.transform.position));
                //collision.collider.GetComponent<Collider>().GetComponent<Health>().TakeDamage(-damage, gameObject);
                col.GetComponent<Collider>().gameObject.SendMessageUpwards("ChangeHealth", -damageAmount, SendMessageOptions.DontRequireReceiver);
                //产生爆炸力
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
