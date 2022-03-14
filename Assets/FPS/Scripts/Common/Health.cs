using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class Health : MonoBehaviour
{
    [Tooltip("Ŀ���������ֵ")]
    public float startHealth = 100.0f;
    [Tooltip("Ŀ�������������ֵ")]
    public float maxHealth = 100.0f;
    [Tooltip("�����滻")]
    public GameObject deadReplacement;

    [Tooltip("����Ч��")]
    public List<GameObject> deadShowers;
    [Tooltip("��ұ�ʾ")]
    public bool isPlayer = false;
    [Tooltip("���������д��ͷ")]
    public GameObject deathCam;

    public UnityAction<float, GameObject> OnDamaged;//�˺�Ч��
    public UnityAction<float> OnHealed;//����Ч��
    public UnityAction OnDie;//����Ч��

    public bool invincible { get; set; }//�Ƿ��޵�
    public float currentHealth { get; set; }
    private bool dead = false;
    // Start is called before the first frame update
    void Start()
    {
        currentHealth = startHealth;
        OnDie += Die;//����Ч����Ҫ���������
    }

    //����
    public void Heal(float healAmount)
    {
        float healthBefore = currentHealth;
        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);//��Чֵ��ȡֵ

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
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);//��Чֵ��ȡֵ
        //ChangeHealth(-damage);

        float trueDamageAmount = healthBefore - currentHealth;
        if (trueDamageAmount > 0f)
        {
            OnDamaged?.Invoke(trueDamageAmount, damageSource);
        }

        HandleDeath();

    }
    //��ɱ
    public void Kill()
    {
        currentHealth = 0f;

        // call OnDamage action
        OnDamaged?.Invoke(maxHealth, null);

        HandleDeath();
    }

    void HandleDeath()
    {
        //����
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
