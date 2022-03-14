using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//�����˺�
[RequireComponent(typeof(Health))]
public class Damageable : MonoBehaviour
{
    [Tooltip("�˺�����")]
    public float DamageMultiplier = 1f;
    [Range(0, 1)]
    [Tooltip("�ѷ��˺�")]
    public float SensibilityToSelfdamage = 0.5f;
    
    public Health Health { get; private set; }//��ȡ��ǰģ���Health
    // Start is called before the first frame update
    void Start()
    {
        Health = GetComponent<Health>();
    }

    public void InflictDamage(float damage, bool isExplosionDamage, GameObject damageSource)
    {
        //�����˺�����
        if (Health)
        {
            damage *= DamageMultiplier;
            if (Health.gameObject == damageSource)
                damage *= SensibilityToSelfdamage;
            Health.TakeDamage(damage, damageSource);
        }
    }
}
