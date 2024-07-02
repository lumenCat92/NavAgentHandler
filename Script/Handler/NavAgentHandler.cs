using System;
using System.Collections;
using System.Runtime.InteropServices;
using LumenCat92.SimpleFSM;
using LumenCat92.TimeCounter;
using UnityEngine;
using UnityEngine.AI;

namespace LumenCat92.Nav
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(NavMeshObstacle))]
    public class NavAgentHandler : MonoBehaviour
    {
        public NavMeshAgent navMeshAgent { private set; get; }
        NavMeshObstacle navMeshObstacle { set; get; }
        NavAgentModuleHandler StateModuleHandler { set; get; }
        public bool AllowedChekingDeadLock { set; get; } = true;
        private Action OnDoneHandler { set; get; }
        bool isInterrupting = false;
        //Gizmos
        [field: SerializeField] public bool ShouldDrawGizmos { set; get; } = false;
        public Func<bool> OnSetDrawingGizmos { get => () => ShouldDrawGizmos; }

        private void Awake()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            navMeshObstacle = GetComponent<NavMeshObstacle>();
            TurnOnAgent(true);

            StateModuleHandler =
                new NavAgentModuleHandler(
                    navMeshAgent,
                    (coroutine) => StartCoroutine(coroutine),
                    (coroutine) => StopCoroutine(coroutine),
                    DoWhenStateDone,
                    OnSetDrawingGizmos
                    );
        }

        public void StartState(NavAgentModule.NavAgentModuleData moduleData, Action onDoneHandler)
        {
            switch (moduleData.State)
            {
                case NavAgentModule.StateList.Pointing:
                case NavAgentModule.StateList.Tracking:
                case NavAgentModule.StateList.Hiding:
                    OnDoneHandler = onDoneHandler;
                    isInterrupting = StateModuleHandler.IsPlayingModuleRunning();
                    TurnOnAgent(true);
                    StateModuleHandler.EnterModule((int)moduleData.State, moduleData);
                    break;
                case NavAgentModule.StateList.Cheking:
                    var checkingModule = StateModuleHandler.GetModule((int)NavAgentModule.StateList.Cheking);
                    if (checkingModule.CanEnter(moduleData))
                    {
                        checkingModule.Enter();
                    }
                    break;
            }
        }

        public void DoWhenStateDone()
        {
            if (!isInterrupting)
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
                navMeshObstacle.enabled = !shouldTurnOn;
                navMeshAgent.enabled = shouldTurnOn;
            }
            else
            {
                navMeshAgent.enabled = shouldTurnOn;
                navMeshObstacle.enabled = !shouldTurnOn;
            }
        }
    }
}