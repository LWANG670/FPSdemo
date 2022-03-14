using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public List<EnemyController> Enemies = new List<EnemyController>();
    public List<Transform> startPos;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q) && Enemies.Count > 0)
        {
            int i = Random.Range(0, startPos.Count);
            EnemyController enemy =Instantiate(Enemies[0], startPos[i].position, startPos[i].rotation);
            FindObjectOfType<PatrolPath>().addEnemy(enemy);//ÃÌº”π÷ŒÔ
        }
    }
}
