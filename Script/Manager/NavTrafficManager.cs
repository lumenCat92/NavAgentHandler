using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;
using LumenCat92.SimpleFSM;
using LumenCat92.Extentioner;
using LumenCat92.GizmosDrawer;
using Unity.Android.Gradle.Manifest;

namespace LumenCat92.Nav
{
    public class NavTrafficManager : MonoBehaviour
    {
        private List<TrafficData> trafficDatas = new List<TrafficData>();
        private List<TrafficData> removeTrafficDataList = new List<TrafficData>();
        public static NavTrafficManager Instance { private set; get; }
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        public TrafficData GetLightTrafficNearBy(Vector3 position, float castRadius)
        {
            var targetData = trafficDatas.Find(x =>
            {
                var dist = Vector3.Distance(x.Position, position);
                return dist <= castRadius;
            });

            return targetData;
        }
        public bool TryAddTrafficNearBy(NavMeshAgent agent, object sessionKey, Func<object, bool> IsSessionRunning, int maxAddingCount, float allowedStayingTime, float stayingTime, Vector3 position, float castRadius)
        {
            TrafficData targetData = null;

            for (int i = 0; i < trafficDatas.Count; i++)
            {
                var data = trafficDatas[i];
                if (data.IsInfoEmpty)
                {
                    trafficDatas.RemoveAt(i--);
                    continue;
                }

                var dist = data.Position.GetDistance(position);
                if (dist < castRadius)
                {
                    targetData = data;
                    break;
                }
            }

            if (targetData == null)
            {
                targetData = new TrafficData(maxAddingCount, allowedStayingTime, position);
            }

            var canAdd = targetData.CanAdd(agent, stayingTime);
            if (canAdd)
                targetData.AddingAgent(agent, sessionKey, castRadius, IsSessionRunning);

            AddData(targetData);
            return canAdd;
        }

        void AddData(TrafficData data)
        {
            if (data.IsNew)
            {
                var info = data.GetNextInfo();
                trafficDatas.Add(data);
                StartCoroutine(HandlingTraffic(info, data));
            }
        }

        IEnumerator HandlingTraffic(TrafficData.NavAgentModuleInfoData infoData, TrafficData trafficData)
        {
            if (infoData == null)
            {
                yield break;
            }
            else
            {
                if (!infoData.IsSessionRunning()) { StartCoroutine(HandlingTraffic(trafficData.GetNextInfo(), trafficData)); yield break; }
                infoData.Agent.stoppingDistance = infoData.stoppingDistance;
                infoData.Agent.avoidancePriority = 0;
                float checkingTime = 0.1f;

                for (bool isSessionRunning = infoData.IsSessionRunning(); isSessionRunning; isSessionRunning = infoData.IsSessionRunning())
                {
                    yield return new WaitForSeconds(checkingTime);
                }

                if (trafficData.HasNextInfo())
                {
                    for (float dist = trafficData.Position.GetDistance(infoData.Agent.transform.position); dist < infoData.CastRadius * 2.5f; dist = trafficData.Position.GetDistance(infoData.Agent.transform.position))
                    {
                        yield return new WaitForSeconds(checkingTime);
                    }
                }

                StartCoroutine(HandlingTraffic(trafficData.GetNextInfo(), trafficData));
            }
        }

        public class TrafficData
        {
            public bool IsNew { set; get; } = true;
            public int MaxAddingCount { private set; get; } = 3;
            public float AllowedStayingTime { private set; get; } = 3f;
            public bool IsLimitedStayingTime { get => AllowedStayingTime != -1; }
            private List<NavAgentModuleInfoData> InfoList { set; get; } = new List<NavAgentModuleInfoData>();
            public Vector3 Position { private set; get; } = Vector3.zero;
            public int AddingCount { private set; get; } = 0;
            public bool IsInfoEmpty { private set; get; } = false;
            public Color GizmosColor { private set; get; }
            public TrafficData(int maxAddingCount, float allowedStayingTime, Vector3 position)
            {
                IsNew = true;
                MaxAddingCount = maxAddingCount;
                AllowedStayingTime = allowedStayingTime;
                Position = position;
                var r = UnityEngine.Random.Range(0, 1);
                var g = UnityEngine.Random.Range(0, 1);
                var b = UnityEngine.Random.Range(0, 1);
                GizmosColor = new Color(r, g, b, 1);
            }
            public void AddingAgent(NavMeshAgent agent, object sessionKey, float castRadius, Func<object, bool> IsSessionRunning)
            {
                InfoList.Add(new NavAgentModuleInfoData(agent, sessionKey, castRadius, IsSessionRunning));
                agent.avoidancePriority = AddingCount++;
                agent.stoppingDistance += agent.transform.lossyScale.x * agent.radius * 2.5f;
            }

            public bool CanAdd(NavMeshAgent agent, float stayingTime)
            {
                if (IsInfoEmpty) return false;
                if (InfoList.Count >= MaxAddingCount) return false;
                if (InfoList.Any(x => x.Agent == agent)) return false;
                if (IsLimitedStayingTime && stayingTime == -1) return false;
                if (IsLimitedStayingTime && stayingTime > AllowedStayingTime) return false;

                return true;
            }

            public NavAgentModuleInfoData GetNextInfo()
            {
                IsNew = false;
                if (InfoList.Count > 0)
                {
                    var data = InfoList[0];
                    InfoList.RemoveAt(0);
                    return data;
                }
                else
                {
                    IsInfoEmpty = true;
                    return null;
                }
            }

            public bool HasNextInfo()
            {
                return InfoList.Count > 0;
            }

            public class NavAgentModuleInfoData
            {
                public NavMeshAgent Agent { private set; get; }
                private object SessionKey { set; get; }
                public float CastRadius { private set; get; }
                private Func<object, bool> OnIsSessionRunning { set; get; }
                public float stoppingDistance { private set; get; }
                public bool IsSessionRunning() => OnIsSessionRunning(SessionKey);
                public NavAgentModuleInfoData(NavMeshAgent agent, object sessionKey, float castRadius, Func<object, bool> isSessionRunning)
                {
                    Agent = agent;
                    SessionKey = sessionKey;
                    CastRadius = castRadius;
                    OnIsSessionRunning = isSessionRunning;
                    stoppingDistance = agent.stoppingDistance;
                }
            }
        }
    }
}