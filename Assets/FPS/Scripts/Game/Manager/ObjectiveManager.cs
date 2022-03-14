using System.Collections.Generic;
using UnityEngine;

namespace Unity.FPS.Game
{
    //游戏任务管理器
    public class ObjectiveManager : MonoBehaviour
    {
        //任务清单
        List<Objective> m_Objectives = new List<Objective>();
        bool m_ObjectivesCompleted = false;

        void Awake()
        {
            Objective.OnObjectiveCreated += RegisterObjective;
        }

        void RegisterObjective(Objective objective) => m_Objectives.Add(objective);

        void Update()
        {
            if (m_Objectives.Count == 0 || m_ObjectivesCompleted)
                return;

            for (int i = 0; i < m_Objectives.Count; i++)
            {
                if (m_Objectives[i].IsBlocking())
                {
                    return;
                }
            }
            //完成所有任务
            m_ObjectivesCompleted = true;
            //会调用游戏结束
            EventManager.Broadcast(Events.AllObjectivesCompletedEvent);
        }

        void OnDestroy()
        {
            Objective.OnObjectiveCreated -= RegisterObjective;
        }
    }
}