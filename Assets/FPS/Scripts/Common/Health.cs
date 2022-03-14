using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Health : MonoBehaviour
{
    [Tooltip("目标出身生命值")]
    public float startHealth = 100.0f;
    [Tooltip("目标物体最大生命值")]
    public float maxHealth = 100.0f;
    [Tooltip("死亡替换")]
    public GameObject deadReplacement;

    [Tooltip("死亡效果")]
    public List<GameObject> deadShowers;
    [Tooltip("玩家标示")]
    public bool isPlayer = false;
    [Tooltip("玩家死亡特写镜头")]
    public GameObject deathCam;

    public UnityAction<float, GameObject> OnDamaged;//伤害效果
    public UnityAction<float> OnHealed;//治疗效果
    public UnityAction OnDie;//死亡效果

    public bool invincible { get; set; }//是否无敌
    public float currentHealth { get; set; }
    private bool dead = false;
    // Start is called before the first frame update
    void Start()
    {
        currentHealth = startHealth;
        OnDie += Die;//死亡效果需要在这添加吗
    }

    //治疗
    public void Heal(float healAmount)
    {
        float healthBefore = currentHealth;
        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);//有效值间取值

        float trueHealAmount = currentHealth - healthBefore;
        if (trueHealAmount > 0f)
        {
            OnHealed?.Invoke(trueHealAmount);
        }
    }

    public void TakeDamage(float damage, GameObject damageSource)
    {
        if (invincible)
            return;

        float healthBefore = currentHealth;
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);//有效值间取值
        //ChangeHealth(-damage);

        float trueDamageAmount = healthBefore - currentHealth;
        if (trueDamageAmount > 0f)
        {
            OnDamaged?.Invoke(trueDamageAmount, damageSource);
        }

        HandleDeath();

    }
    //必杀
    public void Kill()
    {
        currentHealth = 0f;

        // call OnDamage action
        OnDamaged?.Invoke(maxHealth, null);

        HandleDeath();
    }

    void HandleDeath()
    {
        //死亡
        if (dead)
            return;

        if (currentHealth <= 0f)
        {
            dead = true;
            OnDie?.Invoke();
        }
    }
    public void Die()
    {
        dead = true;
        if (deadReplacement != null)
            Instantiate(deadReplacement, transform.position, transform.rotation);
        if (deadShowers != null)
        {
            foreach(var deadShower in deadShowers)
                Instantiate(deadShower, transform.position, transform.rotation);
        }
        if (isPlayer && deathCam != null)
        {
            deathCam.SetActive(true);
        }
        if(isPlayer)
            SceneManager.LoadScene("LossScene");

        Destroy(gameObject);
    }
}
