using System;
using System.Collections;
using System.Collections.Generic;
using LumenCat92.Extentioner;
using UnityEngine;
using UnityEngine.AI;
using LumenCat92.TimeCounter;
using LumenCat92.GizmosDrawer;

namespace LumenCat92.Nav
{
    public class PointingState_NavStateModule : NavAgentModule
    {
        public PointingState_NavStateModule(NavAgentModuleHandler handler) : base(handler) { }
        protected override IEnumerator OnStartNav(NavAgentModuleData data)
        {
            var hasBeenCheckNearBy = false;
            var hasAddedTrafficData = false;

            var findWay = TrySetDestination(data.BasicModulerSetting.Target.position, ref correctedPosition);
            if (!findWay)
            {
                data.BasicModulerSetting.OnFailedToFindWay.Invoke();
                yield break;
            }

            var state = GetStateByDist;
            while (IsModuleRunning && state != DistStateList.Reached)
            {
                state = GetStateByDist;
                if (state == DistStateList.Close &&
                    !hasBeenCheckNearBy)
                {
                    hasBeenCheckNearBy = true;
                    //try add traffic point first
                    if (hasAddedTrafficData == false &&
                        NavTrafficManager.Instance.TryAddTrafficNearBy(
                                        agent: Agent,
                                        sessionKey: ModuleData,
                                        IsSessionRunning: (key) => { return key == ModuleData; },
                                        maxAddingCount: ModuleData.PointingModulerSetting.TrafficDataSetting.MaxWaitingCount,
                                        allowedStayingTime: ModuleData.PointingModulerSetting.TrafficDataSetting.MaxStayingTime,
                                        stayingTime: ModuleData.PointingModulerSetting.StayingTime,
                                        position: correctedPosition,
                                        castRadius: GetAgentRadius * 2.5f))
                    {
                        // after added to traffic manager, traffic Manager will handle agent.
                        hasAddedTrafficData = true;
                    }
                    // if it failed to added to traffic manager, agent should handle the problem it-self.
                    else if (hasAddedTrafficData == false)
                    {
                        // if it can change position
                        if (data.PointingModulerSetting.CanPositionChange)
                        {

                            // try to reach to near position
                            if (state == DistStateList.Near)
                                break;
                            // it will set near position
                            var rayStartPoint = Agent.transform.position + Vector3.up * GetAgentHeight * 0.5f;
                            var rayDir = rayStartPoint.GetDirection(ModuleData.BasicModulerSetting.Target.position);
                            OnDrawGizmosSphere(rayStartPoint, 0.02f, 2f, Color.cyan);
                            OnDrawGizmosLine(rayStartPoint, rayDir, 2f, 2f, Color.black);
                            if (rayStartPoint.RayCast(rayDir, out RaycastHit hit))
                            {
                                var position = hit.point + -rayDir * GetAgentRadius * 2f;
                                OnDrawGizmosSphere(position, 0.02f, 2f, Color.black);
                                TrySetDestination(position, ref correctedPosition);
                                TimeCounterManager.Instance.SetTimeCounting(0.1f, () => { hasBeenCheckNearBy = false; });
                            }
                            else
                            {
                                Debug.Log("ray Failed");
                                ModuleData.BasicModulerSetting.OnFailedToFindWay?.Invoke();
                            }
                        }
                        else
                        {
                            ModuleData.BasicModulerSetting.OnFailedToFindWay?.Invoke();
                            break;
                        }
                    }
                }

                yield return new WaitForSeconds(NavCheckingTime);
            }

            Debug.Log(Agent.transform.name + " reached");
        }
    }
}