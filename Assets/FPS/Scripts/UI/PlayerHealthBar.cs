using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    public Image HealthFillImage;
    Health m_PlayerHealth;

    // Start is called before the first frame update
    void Start()
    {
        PlayerBehavior playerBehavior = GameObject.FindObjectOfType<PlayerBehavior>();
        m_PlayerHealth = playerBehavior.GetComponent<Health>();
    }

    // Update is called once per frame
    void Update()
    {
        HealthFillImage.fillAmount = m_PlayerHealth.currentHealth / m_PlayerHealth.maxHealth;
    }
}
