using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using LumenCat92.SimpleFSM;
using Unity.VisualScripting;

namespace LumenCat92.Nav
{
    public class NavAgentModuleHandler : StateModuleHandler
    {
        public NavMeshAgent navMeshAgent { private set; get; }
        public Func<IEnumerator, Coroutine> OnStartCoroutine { private set; get; }
        public Action<Coroutine> OnStopCoroutine { private set; get; }
        public Action<StateModule.StateModuleData> OnDoneWork { private set; get; }
        public Func<bool> OnDrawGizmos { private set; get; }
        public NavAgentModuleHandler(NavMeshAgent agent, Func<IEnumerator, Coroutine> onStartCoroutine, Action<Coroutine> onStopCoroutine, Action<StateModule.StateModuleData> onDoneWork, Func<bool> onDrawGizmos)
        {
            navMeshAgent = agent;
            OnStartCoroutine = onStartCoroutine;
            OnStopCoroutine = onStopCoroutine;
            OnDoneWork = onDoneWork;
            OnDrawGizmos = onDrawGizmos;

            base.Modules = NavAgentModule.GetAllList(this);
        }
    }
}