using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;
using System.Linq;
using LumenCat92.SimpleFSM;
using LumenCat92.TimeCounter;
using LumenCat92.Extentioner;
using LumenCat92.GizmosDrawer;

namespace LumenCat92.Nav
{
    public abstract partial class NavAgentModule : StateModule
    {
        protected NavAgentModuleHandler StateModuleHandler { set; get; }
        protected NavMeshAgent Agent { get => StateModuleHandler.navMeshAgent; }
        protected float GetAgentHeight { get => Agent.transform.lossyScale.y * Agent.height; }
        protected float GetAgentRadius { get => Agent.transform.lossyScale.x * Agent.radius; }
        protected float NavCheckingTime = 0.1f;
        protected bool isFindWay = true;
        public enum DistStateList { Reached, Near, Close, MiddleWay }
        //dist
        protected static List<KeyValuePair<DistStateList, float>> eachStateDist = new List<KeyValuePair<DistStateList, float>>();
        protected DistStateList GetStateByDist
        {
            get
            {
                isFindWay = true;
                var navAgent = StateModuleHandler.navMeshAgent;
                if (navAgent.pathPending) return DistStateList.MiddleWay;
                var standingPosition = navAgent.transform.position.GetOverrideY(0);
                var destinationPosition = navAgent.destination.GetOverrideY(0);
                var dist = standingPosition.GetDistance(destinationPosition) - ModuleData.BasicModulerSetting.StopDist;
                if (dist <= GetDistByState(DistStateList.Close))
                {
                    // is destination placed higher than agent?
                    if (correctedPosition.y > Agent.transform.position.y + GetAgentHeight &&
                        correctedPosition.y < Agent.transform.position.y - GetAgentHeight &&
                        Agent.remainingDistance > GetDistByState(DistStateList.Close))
                    {
                        return DistStateList.MiddleWay;
                    }

                    for (int i = 0; i < eachStateDist.Count; i++)
                    {
                        var pair = eachStateDist[i];
                        if (dist <= pair.Value)
                        {
                            return pair.Key;
                        }
                    }
                }

                return DistStateList.MiddleWay;
            }
        }
        protected float GetDistByState(DistStateList state)
        {
            foreach (var item in eachStateDist)
            {
                if (item.Key.Equals(state)) return item.Value;
            }

            return -1f;
        }
        public NavAgentModule(NavAgentModuleHandler handler)
        {
            StateModuleHandler = handler;
            if (eachStateDist.Count == 0)
            {
                eachStateDist.Add(new KeyValuePair<DistStateList, float>(DistStateList.Reached, GetAgentRadius * 0.01f));
                eachStateDist.Add(new KeyValuePair<DistStateList, float>(DistStateList.Near, GetAgentRadius * 2f));
                eachStateDist.Add(new KeyValuePair<DistStateList, float>(DistStateList.Close, GetAgentRadius * 4f));
            }
        }

        new public NavAgentModuleData ModuleData { get => base.ModuleData as NavAgentModuleData; }
        protected Coroutine ProcessingState { set; get; }
        // protected TimeCounterManager.TimeCountData crowededTimeData = null;
        protected TimeCounterManager.TimeCountData deadLockTimeData = null;
        protected Vector3 correctedPosition = Vector3.zero;
        public enum StateList
        {
            Pointing,
            Tracking,
            Hiding,
            Cheking,
            Non,
        }
        public static List<StateModule> GetAllList(NavAgentModuleHandler handler)
        {
            var list = new List<StateModule>();
            for (StateList i = StateList.Pointing; i < StateList.Non; i++)
            {
                switch (i)
                {
                    case StateList.Pointing: list.Add(new PointingState_NavStateModule(handler)); break;
                    case StateList.Tracking: list.Add(new TrackingState_NavStateModule(handler)); break;
                    case StateList.Hiding: list.Add(new HidingState_NavStateModule(handler)); break;
                    case StateList.Cheking: list.Add(new CheckingState_NavStateModule(handler)); break;
                }
            }
            return list;
        }

        public override bool IsReady() { return ModuleData != null; }
        protected override void OnEnterModule()
        {
            Agent.updateRotation = ModuleData.BasicModulerSetting.IsLookAtOn ? false : true;
            Agent.stoppingDistance = ModuleData.BasicModulerSetting.StopDist;
            Agent.speed = ModuleData.BasicModulerSetting.AgentSpeed;
            Agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            Agent.avoidancePriority = 10;
            var needNavi = Vector3.Distance(ModuleData.BasicModulerSetting.Target.position, Agent.transform.position) > eachStateDist[(int)DistStateList.Reached].Value;
            if (needNavi)
            {
                //StateModuleHandler.OnTurnOnAgent(true);
                // CheckingCroweded();
                if (ModuleData.BasicModulerSetting.ShouldActiveDeadLockCheck)
                    CheckingDeadLock();

                ProcessingState = StateModuleHandler.OnStartCoroutine(StartNav(ModuleData, base.Exit));
            }
            else
            {
                base.Exit();
            }
        }

        protected bool TrySetDestination(Vector3 targetPosition, ref Vector3 correctiondVector)
        {
            if (IsPositionCanReach(targetPosition, out NavMeshHit hit))
            {
                correctiondVector = hit.position;
                Agent.SetDestination(hit.position);
                return true;
            }
            else
            {
                ModuleData.BasicModulerSetting.OnFailedToFindWay.Invoke();
                return false;
            }
        }

        protected bool IsPositionCanReach(Vector3 targetPosition, out NavMeshHit hit, float maxRadius = -1f)
        {
            hit = new NavMeshHit();
            var filter = new NavMeshQueryFilter
            {
                agentTypeID = Agent.agentTypeID,
                areaMask = NavMesh.AllAreas
            };

            if (maxRadius < 0f)
            {
                for (float searchRadius = 0f, increasingRadius = 5f; searchRadius < 25f; searchRadius += increasingRadius)
                {
                    if (NavMesh.SamplePosition(targetPosition, out hit, searchRadius, filter))
                    {
                        OnDrawGizmosSphere(hit.position, 0.02f, 2f, Color.blue);
                        return true;
                    }
                    else
                    {
                        Debug.Log(Agent.name + " failed " + searchRadius);
                    }
                }
            }
            else
            {
                if (NavMesh.SamplePosition(targetPosition, out hit, maxRadius, filter))
                {
                    return true;
                }
            }

            return false;
        }

        // protected void CheckingCroweded()
        // {
        //     crowededTimeData = TimeCounterManager.Instance.SetTimeCounting(
        //                                         maxTime: 0.5f,
        //                                         function: () =>
        //                                             {
        //                                                 Agent.obstacleAvoidanceType = IsCrowededByNearObj(out Collider[] hitCollider) ? ObstacleAvoidanceType.NoObstacleAvoidance : ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        //                                                 crowededTimeData = null;
        //                                                 if (ModuleData != null) CheckingCroweded();
        //                                             },
        //                                         sequenceKey: ModuleData,
        //                                         sequnceMatch: (addedSeqeunce) => addedSeqeunce.Equals(ModuleData));
        // }
        protected bool IsCrowededByNearObj(out Collider[] hitColliders)
        {
            var isCroweded = false;
            var distByState = GetDistByState(DistStateList.Close);
            hitColliders = Physics.OverlapSphere(Agent.transform.position, distByState);
            var count = 0;
            var maxCount = 3;
            foreach (var collider in hitColliders)
            {
                if (collider.gameObject.isStatic) continue;

                count++;
                if (count > maxCount)
                {
                    isCroweded = true;
                    break;
                }
            }

            return isCroweded;
        }


        protected void CheckingDeadLock()
        {
            var eachTime = 0.1f;
            var lastAgentPosition = Agent.transform.position;
            var speedCorrection = 0.7f;
            var expectingDist = Agent.transform.position.GetDistance(Agent.transform.position + Agent.velocity * eachTime * speedCorrection);
            deadLockTimeData = TimeCounterManager.Instance.SetTimeCounting(
                maxTime: eachTime,
                function: () =>
                    {
                        if (Agent.obstacleAvoidanceType == ObstacleAvoidanceType.NoObstacleAvoidance)
                        {
                            var hits = Agent.transform.GetOverlapSphere(GetAgentRadius).Where(x => !x.gameObject.isStatic && !x.isTrigger && !x.transform.IsChildOf(Agent.transform)).ToList();
                            if (hits.Count == 0)
                                Agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                        }
                        else
                        {
                            var dist = Agent.transform.position.GetDistance(lastAgentPosition);
                            if (dist < expectingDist)
                            {
                                Agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                                GizmosDrawerManager.Instance.DrawSphere(Agent.transform.position + Vector3.up * 1f, 0.02f, 2f, Color.red);
                            }
                        }

                        deadLockTimeData = null;
                        if (ModuleData != null) CheckingDeadLock();
                    },
                sequenceKey: ModuleData,
                sequnceMatch: (addedSeqeunce) => addedSeqeunce.Equals(ModuleData));
        }

        protected void FaildFindWay()
        {
            isFindWay = false;
            ModuleData.BasicModulerSetting.OnFailedToFindWay?.Invoke();
        }
        protected override void OnExitModule()
        {
            if (ProcessingState != null)
            {
                StateModuleHandler.OnStopCoroutine(ProcessingState);
            }

            Agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            Agent.isStopped = true;
            Agent.avoidancePriority = 99;
            Agent.stoppingDistance = 100f;
            base.ModuleData = null;
            TimeCounterManager.Instance.StopTimeCounting(deadLockTimeData);
            if (isFindWay)
                StateModuleHandler.OnDoneWork?.Invoke();
        }

        public override void EnterModuleToException() { }
        protected IEnumerator StartNav(NavAgentModuleData data, Action DoWhenDone)
        {
            Agent.isStopped = false;
            yield return StateModuleHandler.OnStartCoroutine(OnStartNav(data));
            // var leftDist = Agent.transform.position.GetDistance(correctedPosition);
            // var dir = Agent.transform.position.GetOverrideY(0).GetDirection(correctedPosition.GetOverrideY(0));
            // var leftTime = leftDist / Agent.speed;
            // var eachTime = leftTime / Time.fixedDeltaTime;
            // var lerpTime = Mathf.InverseLerp(0, leftTime, eachTime);
            // var eachDist = Mathf.Lerp(0, leftTime, lerpTime);
            // for (float time = 0f; time < leftTime; time += eachTime)
            // {
            //     Agent.transform.position += dir * eachDist;
            //     yield return new WaitForSeconds(eachTime);
            // }
            DoWhenDone.Invoke();
        }
        protected abstract IEnumerator OnStartNav(NavAgentModuleData data);

        //gizmos 
        public void OnDrawGizmosLine(Vector3 startPosition, Vector3 dir, float dist, float duration, Color color)
            => GizmosDrawerManager.Instance.DrawLine(startPosition, dir, dist, duration, color, StateModuleHandler.OnDrawGizmos.Invoke());
        public void OnDrawGizmosLine(Vector3 startPosition, Vector3 endPoint, float duration, Color color)
            => GizmosDrawerManager.Instance.DrawLine(startPosition, endPoint, duration, color, StateModuleHandler.OnDrawGizmos.Invoke());
        public void OnDrawGizmosSphere(Vector3 startPosition, float size, float duration, Color color)
            => GizmosDrawerManager.Instance.DrawSphere(startPosition, size, duration, color, StateModuleHandler.OnDrawGizmos.Invoke());
    }
}