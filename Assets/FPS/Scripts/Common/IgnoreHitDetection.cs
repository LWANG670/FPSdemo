using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IgnoreHitDetection : MonoBehaviour
{
    //���ڲ����յ��˺��Ķ�����ʱ��Ӻ�ɾ����
    // Start is called before the first frame update
    public float Life { get; set; }
    float m_CurTime;
    void Start()
    {
        m_CurTime = 0;
        Life = 9999999f;
    }

    // Update is called once per frame
    void Update()
    {
        m_CurTime += Time.deltaTime;
        if(m_CurTime> Life)
            Destroy(gameObject.GetComponent<IgnoreHitDetection>());
    }

    
}
