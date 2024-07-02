using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using LumenCat92.Extentioner;
using LumenCat92.TimeCounter;

namespace LumenCat92.Nav
{
    // tracking_state cant get out state it-self cause it's keep tracking.
    // cause of this, DoWhenCantFindWay also not support.
    public class TrackingState_NavStateModule : NavAgentModule
    {
        public TrackingState_NavStateModule(NavAgentModuleHandler handler) : base(handler) { }

        protected override IEnumerator OnStartNav(NavAgentModuleData data)
        {
            var hasBeenCheckNearBy = false;

            var lastPosition = data.BasicModulerSetting.Target.position;
            if (!TrySetDestination(lastPosition, ref correctedPosition))
            {
                data.BasicModulerSetting.OnFailedToFindWay?.Invoke();
                yield break;
            }

            var state = GetStateByDist;
            while (ModuleData.TrackingModulerSetting.IsNonStopTracking ||
                    (!ModuleData.TrackingModulerSetting.IsNonStopTracking && state != DistStateList.Reached))
            {
                state = GetStateByDist;
                //if target Moved, position will be re-calc
                if (lastPosition.GetDistance(data.BasicModulerSetting.Target.position) > 0.1f)
                {
                    lastPosition = data.BasicModulerSetting.Target.position;
                    if (!TrySetDestination(lastPosition, ref correctedPosition))
                    {
                        data.BasicModulerSetting.OnFailedToFindWay?.Invoke();
                        yield break;
                    }
                }

                // can it yield when it get close to target
                if (state == DistStateList.Close || state == DistStateList.Near)
                {
                    if (!hasBeenCheckNearBy)
                    {
                        hasBeenCheckNearBy = true;
                        var rayStartPoint = Agent.transform.position + Vector3.up * Agent.transform.lossyScale.y * Agent.height * 0.5f;
                        var rayDir = Agent.transform.position.GetOverrideY(0).GetDirection(ModuleData.BasicModulerSetting.Target.position.GetOverrideY(0));

                        if (rayStartPoint.RayCast(rayDir, out RaycastHit hit))
                        {
                            var position = hit.point + rayDir * -Agent.radius * 1.2f * Agent.transform.lossyScale.x;
                            OnDrawGizmosSphere(position, 0.02f, 2f, Color.black);
                            Agent.stoppingDistance = Vector3.Distance(ModuleData.BasicModulerSetting.Target.position, position);
                            TimeCounterManager.Instance.SetTimeCounting(0.1f, () => { hasBeenCheckNearBy = false; });
                        }
                    }
                }
                else
                {
                    if (Agent.stoppingDistance != ModuleData.BasicModulerSetting.StopDist)
                        Agent.stoppingDistance = ModuleData.BasicModulerSetting.StopDist;
                }
                yield return new WaitForSeconds(NavCheckingTime);
            }
        }
    }
}