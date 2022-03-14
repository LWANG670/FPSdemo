using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//处理伤害
[RequireComponent(typeof(Health))]
public class Damageable : MonoBehaviour
{
    [Tooltip("伤害倍率")]
    public float DamageMultiplier = 1f;
    [Range(0, 1)]
    [Tooltip("友方伤害")]
    public float SensibilityToSelfdamage = 0.5f;
    
    public Health Health { get; private set; }//获取当前模块的Health
    // Start is called before the first frame update
    void Start()
    {
        Health = GetComponent<Health>();
    }

    public void InflictDamage(float damage, bool isExplosionDamage, GameObject damageSource)
    {
        //处理伤害属性
        if (Health)
        {
            damage *= DamageMultiplier;
            if (Health.gameObject == damageSource)
                damage *= SensibilityToSelfdamage;
            Health.TakeDamage(damage, damageSource);
        }
    }
}
