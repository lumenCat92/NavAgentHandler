using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace LumenCat92.Nav
{
    public class CheckingState_NavStateModule : NavAgentModule
    {
        public CheckingState_NavStateModule(NavAgentModuleHandler handler) : base(handler) { }
        public override bool IsReady()
        {
            return base.IsReady() && ModuleData.CheckingMoudlerSetting.OnFindWay != null;
        }
        protected override IEnumerator OnStartNav(NavAgentModuleData data)
        {
            NavMeshPath path = new NavMeshPath();
            if (Agent.CalculatePath(data.BasicModulerSetting.Target.position, path))
            {
                if (path.status == NavMeshPathStatus.PathComplete)
                {
                    data.CheckingMoudlerSetting.OnFindWay(true);
                    yield break;
                }
            }

            data.CheckingMoudlerSetting.OnFindWay(false);
            yield break;
        }
    }
}