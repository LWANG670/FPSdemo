using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Actor : MonoBehaviour
{
    public int Affiliation;

    public Transform AimPoint;

    ActorsManager m_ActorsManager;
    // Start is called before the first frame update
    void Start()
    {
        m_ActorsManager= GameObject.FindObjectOfType<ActorsManager>();
        if (!m_ActorsManager.Actors.Contains(this))
        {
            m_ActorsManager.Actors.Add(this);
        }
    }

    //œ˙ªŸ π”√
    private void OnDestroy()
    {
        if(m_ActorsManager)
            m_ActorsManager.Actors.Remove(this);
    }
}
