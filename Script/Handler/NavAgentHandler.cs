using System;
using LumenCat92.SimpleFSM;
using UnityEngine;
using UnityEngine.AI;

namespace LumenCat92.Nav
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(NavMeshObstacle))]
    public class NavAgentHandler : MonoBehaviour
    {
        public NavMeshAgent NavMeshAgent { private set; get; }
        NavMeshObstacle NavMeshObstacle { set; get; }
        NavAgentModuleHandler StateModuleHandler { set; get; }
        public bool AllowedChekingDeadLock { set; get; } = true;
        private Action OnDoneHandler { set; get; }
        StateModule.StateModuleData LastStateModuleData { set; get; }
        //Gizmos
        [field: SerializeField] public bool ShouldDrawGizmos { set; get; } = false;
        public Func<bool> OnSetDrawingGizmos { get => () => ShouldDrawGizmos; }

        private void Awake()
        {
            NavMeshAgent = GetComponent<NavMeshAgent>();
            NavMeshObstacle = GetComponent<NavMeshObstacle>();
            TurnOnAgent(true);

            StateModuleHandler =
                new NavAgentModuleHandler(
                    NavMeshAgent,
                    (coroutine) => StartCoroutine(coroutine),
                    (coroutine) => StopCoroutine(coroutine),
                    DoWhenStateDone,
                    OnSetDrawingGizmos
                    );
        }

        public void SetSpeed(float speed) => NavMeshAgent.speed = speed;

        public void StartState(NavAgentModule.NavAgentModuleData moduleData, float speed, Action onDoneHandler)
        {
            LastStateModuleData = moduleData;
            switch (moduleData.State)
            {
                case NavAgentModule.StateList.Pointing:
                case NavAgentModule.StateList.Tracking:
                case NavAgentModule.StateList.Hiding:
                    OnDoneHandler = onDoneHandler;
                    TurnOnAgent(true);
                    StateModuleHandler.EnterModule((int)moduleData.State, moduleData);
                    SetSpeed(speed);
                    break;
                case NavAgentModule.StateList.Cheking:
                    if (NavMesh.SamplePosition(moduleData.BasicModulerSetting.Target.position, out NavMeshHit hit, moduleData.BasicModulerSetting.StopDist, NavMeshAgent.areaMask))
                    {
                        moduleData.CheckingMoudlerSetting.OnFindWay(true);
                        return;
                    }
                    moduleData.CheckingMoudlerSetting.OnFindWay(false);
                    break;
                case NavAgentModule.StateList.Non:
                    OnDoneHandler = onDoneHandler;
                    if (StateModuleHandler.IsPlayingModuleRunning(out StateModule module))
                    {
                        module?.Exit();
                    }
                    DoWhenStateDone(LastStateModuleData);
                    break;
            }
        }

        private void DoWhenStateDone(StateModule.StateModuleData stateModuleData)
        {
            if (ReferenceEquals(stateModuleData, LastStateModuleData))
            {
                TurnOnAgent(false);
                OnDoneHandler?.Invoke();
            }
        }

        private void TurnOnAgent(bool shouldTurnOn)
        {
            //dont change the sequence.
            if (shouldTurnOn)
            {
                NavMeshObstacle.enabled = !shouldTurnOn;
                NavMeshAgent.enabled = shouldTurnOn;
            }
            else
            {
                NavMeshAgent.enabled = shouldTurnOn;
                NavMeshObstacle.enabled = !shouldTurnOn;
            }
        }

        public Vector3 GetNaviDirection()
        {
            var direction = Vector3.zero;
            if (NavMeshAgent != null)
            {
                if (NavMeshAgent.isOnNavMesh
                        && !NavMeshAgent.isStopped)
                {
                    direction = NavMeshAgent.velocity.normalized;
                }
            }

            return direction;
        }
    }
}