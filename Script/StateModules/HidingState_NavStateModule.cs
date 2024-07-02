using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using LumenCat92.Extentioner;
using LumenCat92.GizmosDrawer;
namespace LumenCat92.Nav
{
    public class HidingState_NavStateModule : NavAgentModule
    {
        public HidingState_NavStateModule(NavAgentModuleHandler handler) : base(handler) { }
        protected override IEnumerator OnStartNav(NavAgentModuleData data)
        {
            if (!NavHideManager.Instance.TryAddHide(ModuleData.BasicModulerSetting.Target, Agent, out Vector3 hidePosition, ModuleData.HidingMoudlerSetting.HidableLayer))
            {
                FaildFindWay();
                yield break;
            }

            else if (!TrySetDestination(hidePosition, ref correctedPosition))
            {
                FaildFindWay();
                yield break;
            }

            while (GetStateByDist != DistStateList.Reached)
            {
                yield return new WaitForSeconds(NavCheckingTime);
            }
        }
    }
}