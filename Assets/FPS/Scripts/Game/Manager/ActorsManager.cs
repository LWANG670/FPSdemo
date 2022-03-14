using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActorsManager : MonoBehaviour
{
    //管理场景中所有的所有可扮演对象
    public List<Actor> Actors { get; private set; }
    public GameObject Player { get; private set; }

    public void SetPlayer(GameObject player) => Player = player;
    void Awake()
    {
        Actors = new List<Actor>();
    }
}
