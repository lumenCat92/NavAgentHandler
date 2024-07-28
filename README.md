# LumenCat
<div align="center">

![LumenCat92.jpg](https://github.com/lumenCat92/NavAgentHandler/blob/main/Image/LumenCat92.jpg)

본 작업은 유니티 엔진을 대상으로 합니다.  
this work target to Unity Engine.
</div>

# NavAgentHandler
 ![NavAgentHandler.jpg](https://github.com/lumenCat92/NavAgentHandler/blob/main/Image/NavAgentHandler.jpg)

# Language
<details>

<summary>English</summary>

# How Can Install This?

Before u install, this project depending other project. plz check each version of dependencies before download this.  
u can check each version of dependencies from package.json.  

Download this to Assets Folder in your unity project.

# What is This?

Handling Nav Agent with FSM.

# Where Can Use This?

if u wanna easy to handling agent, u can use this.

# How to Use This?

1. Attach "NavAgentHandler" to gameObj as component.
* the GameObj that installed NavAgentHandler, will automatically add "NavMeshAgent" and "NavMeshObstacle".
```csharp
namespace LumenCat92.Nav
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(NavMeshObstacle))]
    public class NavAgentHandler : MonoBehaviour
    {
        //skip
    }
    //skip
}
```

2. Attach all script in Manager Folder to other gameObj as component.

3. when u look at the NavAgentHandler

```csharp
public class NavAgentHandler : MonoBehaviour
{    
    // skip
    
    [field: SerializeField] public bool AllowedChekingDeadLock { set; get; } = true;
    [field: SerializeField] public LayerMask HideableLayer { private set; get; }
    private Action OnDoneHandler { set; get; }
    
    // skip
    
    // this is starting navAgentHandler.
    // for the safty, u cant access to process.
    // after state done. u can make it call-back by "onDoneHandler"
    public void StartState(NavAgentModule.NavAgentModuleData moduleData, Action onDoneHandler)
    {
        switch (moduleData.State)
        {
            case NavAgentModule.StateList.Pointing:
            case NavAgentModule.StateList.Tracking:
            case NavAgentModule.StateList.Hiding:
                OnDoneHandler = onDoneHandler;
                isInterrupting = StateModuleHandler.IsPlayingModuleRunning();
                StateModuleHandler.EnterModule((int)moduleData.State, moduleData);
                break;
            
            // checking module is works different cause it just for cheking that agent could reach to target or not.
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

    // but if trying to change the state when state Running,
    // onDoneHandler will be ignored.
    public void DoWhenStateDone()
    {
        if (!isInterrupting)
        {
            OnDoneHandler?.Invoke();
        }
    }
}

```

as u can see, this is for one-way processing, so if u wanna changing one of state in NavAgentModule.NavAgentModuleData.StateList,

u should call StartState() func again. than, navAgentHandler will be override before process.

also, when u override process, it will not execute call-back func that u applied before changed state.

to be clear, when agent cant find way for going destination, it also will not execute call-back func that u applied. but it will call different func in NavAgentModuleData. check num 2. 

2. when u look at the NavAgentModule.NavAgentModuleData for using,

```csharp
public abstract partial class NavAgentModule
{
    public enum StateList
    {
        Pointing, // set destination one time.
        Tracking, // keep tracking target until reached.
        Hiding, // hiding from other target.
        Cheking, // just check agent can reach to target, agent will not move in this state.
        Non,
    }
    public class NavAgentModuleData : StateModuleData
    {
        public BasicOption BasicModulerSetting { private set; get; }
        public PointingOption PointingModulerSetting { private set; get; }
        public CheckingOption CheckingMoudlerSetting { private set; get; }
        public TrackingOption TrackingModulerSetting { private set; get; }
        public HidingOption HidingMoudlerSetting { private set; get; }
        public NavAgentModuleData(BasicOption basicOption, PointingOption pointingOption)
        {
            BasicModulerSetting = basicOption;
            PointingModulerSetting = pointingOption;
        }
        public NavAgentModuleData(BasicOption basicOption, TrackingOption trackingOption)
        {
            BasicModulerSetting = basicOption;
            TrackingModulerSetting = trackingOption;
        }
        public NavAgentModuleData(BasicOption basicOption, HidingOption hidingOption)
        {
            BasicModulerSetting = basicOption;
            HidingMoudlerSetting = hidingOption;
        }
        public NavAgentModuleData(BasicOption basicOption, CheckingOption checkingOption)
        {
            BasicModulerSetting = basicOption;
            CheckingMoudlerSetting = checkingOption;
        }
        
        public class BasicOption
        {
            public Transform Target { private set; get; } // destination point
            public bool ShouldSequenceProcessing { private set; get; } = true; // module process proceeds async
            public bool ShouldRotateManually { private set; get; } // *1) its make agent ratation lock. 
            public float StopDist { private set; get; } // *2) its same with agent stop. but it also effecting dist that end module state.
            public bool ShouldActiveDeadLockCheck { private set; get; } // *3) its for stuck situation between multiple agent. 
            public Action OnFailedToFindWay { private set; get; } // call-back when cant find the way.

            public BasicOption(Transform target, bool shouldSequenceProcessing, bool isLookAtOn, float stopDist, bool shouldActiveDeadLockCheck, Action doWhenCantFindWay)
            {
                Target = target;
                ShouldSequenceProcessing = shouldSequenceProcessing;
                ShouldRotateManually = isLookAtOn;
                StopDist = stopDist;

                ShouldActiveDeadLockCheck = shouldActiveDeadLockCheck;
                OnFailedToFindWay = doWhenCantFindWay;
            }
        }
    }
}
```

    *1 when agent walking, obj rotation depending on agent.

but when u programming, u might be want to control rotation manually for make look at obj or make aiming to obj.

in that time, u can turn on this. but remember. u have to manually control rotation. NavAgentHandler dosent support to make look at the target.(cause this will be different in every situation.)

    *2 StopDist will set to navMeshAgent.StoppingDisctance. and it will effecting final destination. 

in the NavAgentModule, u can see down below
```csharp
public abstract partial class NavAgentModule : StateModule
{
    public enum DistStateList { Reached, Near, Close, MiddleWay }

    //skip

    protected DistStateList GetStateByDist
    {
        get
        {
            var navAgent = StateModuleHandler.navMeshAgent;
            if (navAgent.pathPending) return DistStateList.MiddleWay;
            var standingPosition = navAgent.transform.position.GetOverrideY(0);
            var destinationPosition = navAgent.destination.GetOverrideY(0);

            // dist to decide the "Status Module" done.
            var dist = standingPosition.GetDistance(destinationPosition) - ModuleData.BasicModulerSetting.StopDist;
            if (dist <= GetDistByState(DistStateList.Close))
            {
                // is destination placed higher than agent?
                if (correctedPosition.y > Agent.transform.position.y + GetAgentHeight &&
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

    //skip
}
```

so technically, navMeshAgent.StoppingDistance will be effecting final destination. 

    *3 Lets say, Two agent try to pass each in Narrow hallway.

in this situation, this two agent might never get to each destination by stuck each other.

for this, u can turn on ShouldActiveDeadLockCheck property in BasicModulerSetting.

when u look at the code,

```csharp

public abstract partial class NavAgentModule : StateModule
{
    //skip

    // enter module
    protected override void OnEnterModule()
    {
        Agent.updateRotation = ModuleData.BasicModulerSetting.ShouldRotateManually ? false : true;
        Agent.stoppingDistance = ModuleData.BasicModulerSetting.StopDist;
        Agent.speed = ModuleData.BasicModulerSetting.AgentSpeed;
        Agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        Agent.avoidancePriority = 10;
        var needNavi = Vector3.Distance(ModuleData.BasicModulerSetting.Target.position, Agent.transform.position) > eachStateDist[(int)DistStateList.Reached].Value;
        if (needNavi)
        {
            //checking deadLock
            if (ModuleData.BasicModulerSetting.ShouldActiveDeadLockCheck)
                CheckingDeadLock();

            ProcessingState = StateModuleHandler.OnStartCoroutine(StartNav(ModuleData, base.Exit));
        }
        else
        {
            base.Exit();
        }
    }

    protected void CheckingDeadLock()
    {
        var eachTime = 0.1f;
        var lastAgentPosition = Agent.transform.position;
        var speedCorrection = 0.8f;
        var expectingDist = Agent.transform.position.GetDistance(Agent.transform.position + Agent.velocity * eachTime * speedCorrection);
        deadLockTimeData = TimeCounterManager.Instance.SetTimeCounting(
            maxTime: eachTime,
            function: () =>
                {
                    if (GetStateByDist == DistStateList.MiddleWay)
                    {
                        if (Agent.obstacleAvoidanceType == ObstacleAvoidanceType.NoObstacleAvoidance)
                        {
                            var hits = Agent.transform.GetOverlapSphere(GetAgentRadius).Where(x => !x.gameObject.isStatic && !x.transform.IsChildOf(Agent.transform)).ToList();
                            if (hits.Count == 0)
                                Agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                        }
                        else
                        {
                            var dist = Agent.transform.position.GetDistance(lastAgentPosition);
                            if (dist < expectingDist)
                                Agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                        }
                    }

                    deadLockTimeData = null;
                    if (ModuleData != null) CheckingDeadLock();
                },
            sequenceKey: ModuleData,
            sequnceMatch: (addedSeqeunce) => addedSeqeunce.Equals(ModuleData));
    }
}
```

navAgentModule calc posistion expecting every time period.

if agent couldnt reach next position, after checking near by, module make not collide each agent untill agent cant find other agent.

This can cause more overlapping issues as the number of agents increases. Please pay attention to this part.

# NavAgentModule

now i will explain detail of each state.

<details>
<summary>Pointing</summary>

 ![1.Pointing.gif](https://github.com/lumenCat92/NavAgentHandler/blob/main/Image/1.Pointing.gif)

1. Pointing is basic tracking state module.

its Set Destination only one time. 

even if target position got changed middle of module running, it will not apply to destination.

```csharp
public abstract partial class NavAgentModule
{
    public enum StateList
    {
        Pointing,
        Tracking,
        Hiding,
        Cheking,
        Non,
    }
    public class NavAgentModuleData : StateModuleData
    {
        public BasicOption BasicMudulerSetting { private set; get; }
        public PointingOpition PointModulerSetting { private set; get; } = new PointingOpition();

        // u must fill this for using every tracking module
        public NavAgentModuleData(BasicOption basicOption, PointingOption pointingOption)
        {
            BasicModulerSetting = basicOption;
            PointingModulerSetting = pointingOption;
        }
        
        public class BasicOption
        {
            public Transform Target { private set; get; } // destination point
            public bool ShouldSequenceProcessing { private set; get; } = true; // module process proceeds async
            public bool ShouldRotateManually { private set; get; } // its make agent ratation lock. 
            public float StopDist { private set; get; } // its same with agent stop. but it also effecting dist that end module state.
            public bool ShouldActiveDeadLockCheck { private set; get; } // its for stuck situation between multiple agent. 
            public Action OnFailedToFindWay { private set; get; }

            public BasicOption(Transform target, bool shouldSequenceProcessing, bool isLookAtOn, float stopDist, bool shouldActiveDeadLockCheck, Action doWhenCantFindWay)
            {
                Target = target;
                ShouldSequenceProcessing = shouldSequenceProcessing;
                ShouldRotateManually = isLookAtOn;
                StopDist = stopDist;

                ShouldActiveDeadLockCheck = shouldActiveDeadLockCheck;
                OnFailedToFindWay = doWhenCantFindWay;
            }
        }

        public class PointingOpition
        {
            public float StayingTime { private set; get; } = -1f; // how long it will stay in poisition after reached?
            public bool IsUnlimitedStayTime => StayingTime <= -1;  
            public bool CanPositionChange { private set; get; } = true; // is target position can yield to other agent? 
            public TrafficDataSet TrafficDataSetting { private set; get; } = new TrafficDataSet(); // its for handling traffic
            public PointingOpition() { }
            public PointingOpition(float stayingTime, bool canPositionChange, TrafficDataSet trafficDataSet = null)
            {
                StayingTime = stayingTime;
                CanPositionChange = canPositionChange;
                TrafficDataSetting = trafficDataSet == null ? this.TrafficDataSetting : trafficDataSet;
            }
            public class TrafficDataSet // agent who first reach to position will taking charge of traffic detail of position 
            {
                public int MaxWaitingCount { private set; get; } = 3; // how many agent can wait for same position?
                public float MaxStayingTime { private set; get; } = 3f; // how long agent can stay in same position?
                public TrafficDataSet() { }
                public TrafficDataSet(int maxAddingCount, float maxStayingTime)
                {
                    MaxWaitingCount = maxAddingCount;
                    MaxStayingTime = maxStayingTime;
                }
            }
        }
    }
}
```

2. so, imagine that what if multiple agent try to reach same position?

for this, u need TrafficDataSet and NavTrafficManager.

```csharp
public class NavTrafficManager : MonoBehaviour
{
    //skip

    IEnumerator HandlingTraffic(TrafficData.NavAgentModuleInfoData infoData, TrafficData trafficData)
    {
        if (infoData == null)
        {
            removeTrafficDataList.Add(trafficData);
            yield break;
        }
        else
        {
            if (!infoData.IsModuleRunning()) { StartCoroutine(HandlingTraffic(trafficData.ReadNextInfo(), trafficData)); yield break; }
            infoData.Agent.stoppingDistance = infoData.stoppingDistance;
            infoData.Agent.avoidancePriority = 0;
            float checkingTime = 0.1f;

            for (bool isModuleRunning = infoData.IsModuleRunning(); isModuleRunning; isModuleRunning = infoData.IsModuleRunning())
            {
                yield return new WaitForSeconds(checkingTime);
            }

            for (float dist = trafficData.Position.GetDistance(infoData.Agent.transform.position); dist < 0.5f; dist = trafficData.Position.GetDistance(infoData.Agent.transform.position))
            {
                yield return new WaitForSeconds(checkingTime);
            }

            StartCoroutine(HandlingTraffic(trafficData.ReadNextInfo(), trafficData));
        }
    }

    // skip

    public class TrafficData
    {
        public bool IsNew { private set; get; } = true; 
        public int MaxAddingCount { private set; get; } = 3;
        public float AllowedStayingTime { private set; get; } = 3f;
        public bool IsLimitedStayingTime { get => AllowedStayingTime != -1; }
        private List<NavAgentModuleInfoData> InfoList { set; get; } = new List<NavAgentModuleInfoData>();
        public NavAgentModuleInfoData ReadNextInfo()
        {
            IsNew = false;
            if (InfoList.Count > 0)
            {
                var data = InfoList[0];
                InfoList.RemoveAt(0);
                return data;
            }

            return null;
        }
        public Vector3 Position { private set; get; } = Vector3.zero;
        private int AddingCount { set; get; } = 0;
        
        //skip
    }
}
```

3. but, what if agent dont have to wait? like agent just try to stand, or doing some animation that dosent depending other object after reached position. 

also what if agent must be stand that position? like agent try to sit chair, or doing some animation that depending other object after reached position.

when u look at the code.

```csharp

public class PointingState_NavStateModule : NavAgentModule
{
    // skip

    protected override IEnumerator OnStartNav(NavAgentModuleData data)
    {
        // skip

        var state = GetStateByDist;
        while (state != DistStateList.Reached)
        {
            if (state == DistStateList.Close || state == DistStateList.Near)
            {
                state = GetStateByDist;
                // what if agent dont have to wait? == what if agent can changing destination?
                if (data.PointingModulerSetting.CanPositionChange) 
                {
                    if (state == DistStateList.Near)
                        break;
                    // it will set near position
                    var rayStartPoint = Agent.transform.position + Vector3.up * GetAgentHeight * 0.5f;
                    var rayDir = rayStartPoint.GetDirection(ModuleData.BasicModulerSetting.Target.position);
                    OnDrawGizmosSphere(rayStartPoint, 0.02f, 2f, Color.cyan);
                    OnDrawGizmosLine(rayStartPoint, rayDir, 2f, 2f, Color.black);

                    // try to get close as much as possible. this will be destination.
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
                    }
                }

                // what if agent have to wait? == what if agent cant changing destination?
                else
                {
                    // trying add treffic point
                    // is already has traffic in destination?
                    if (!NavTrafficManager.Instance.TryAddTrafficNearBy(
                            agent: Agent,
                            isModuleRunning: () => { return IsModuleRunning; },
                            maxAddingCount: ModuleData.PointingModulerSetting.TrafficDataSetting.MaxWaitingCount,
                            allowedStayingTime: ModuleData.PointingModulerSetting.TrafficDataSetting.MaxStayingTime,
                            stayingTime: ModuleData.PointingModulerSetting.StayingTime,
                            position: correctedPosition,
                            castRadius: GetAgentRadius))
                    {
                        ModuleData.BasicModulerSetting.OnFailedToFindWay?.Invoke();
                        break;
                    }
                }
            }
        }
    }
}
```
Traffic and deadlock functions are applied.
 ![1.Pointing_TrafficAndDeadLock.gif](https://github.com/lumenCat92/NavAgentHandler/blob/main/Image/1.Pointing_TrafficAndDeadLock.gif)

so when u allowed to change destination, agent try to get close to target as much as possible. but its not guarantee that reach to destination that u set.

if u dont wanna allowed to change destination, agent try to check traffic in destination. if traffic is very busy, it means technically, it faild to searching destination. so it will call-back func that u set.

just remember, this checking will be happen when agent get close to target.

</details>

<details>
<summary>Tracking</summary>

 ![2.Tracking.jpg](https://github.com/lumenCat92/NavAgentHandler/blob/main/Image/2.Tracking.gif)
1. most different thing with comparing pointing, in Tracking module, continues to update the destination based on the position of the target.

```csharp
public class TrackingState_NavStateModule : NavAgentModule
{
    // skip
    protected override IEnumerator OnStartNav(NavAgentModuleData data)
    {
        var hasBeenCheckNearBy = false;

        var lastPosition = data.BasicModulerSetting.Target.position;
        if (!TrySetDestination(lastPosition, ref correctedPosition))
        {
            data.BasicModulerSetting.OnFailedToFindWay?.Invoke();
            yield break;
        }

        // The frequency of looping will be decide by the IsNonStopTracking option.
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
```

2. also its not using traffic mananger. so its not support waiting for position stuff. cause of this, if too many agent try follow one target, some of target might cant reached target, or if u turn on the deadLock option, it might overlap each agent.

3. like pointing module, it will also finished to module when get close to target, but if u want, u can make it keep follow to target no matter it reached or not.

```csharp

public abstract partial class NavAgentModule
{
    public class NavAgentModuleData : StateModuleData
    {
        public BasicOption BasicModulerSetting { private set; get; }
        public TrackingOption TrackingModulerSetting { private set; get; }
        public NavAgentModuleData(BasicOption basicOption, TrackingOption trackingOption)
        {
            BasicModulerSetting = basicOption;
            TrackingModulerSetting = trackingOption;
        }

        public class BasicOption
        {
            public Transform Target { private set; get; }
            public bool ShouldSequenceProcessing { private set; get; } = true;
            public bool ShouldRotateManually { private set; get; }
            public float StopDist { private set; get; }
            public bool ShouldActiveDeadLockCheck { private set; get; }
            public Action OnFailedToFindWay { private set; get; }

            public BasicOption(StateList targetState, Transform target, bool isLookAtOn, float stopDist, float agentSpeed, bool shouldActiveDeadLockCheck, Action doWhenCantFindWay)
            {
                TargetState = targetState;
                Target = target;
                IsLookAtOn = isLookAtOn;
                StopDist = stopDist;
                AgentSpeed = agentSpeed;
                ShouldActiveDeadLockCheck = shouldActiveDeadLockCheck;
                OnFailedToFindWay = doWhenCantFindWay; 
            }
        }

        public class TrackingOption
        {
            public bool IsNonStopTracking { private set; get; }
            public TrackingOption(bool isNonStopTracking)
            {
                IsNonStopTracking = isNonStopTracking;
            }
        }
    }
}
```

</details>

<details>
<summary>Hiding</summary>

 ![3.Hiding.gif](https://github.com/lumenCat92/NavAgentHandler/blob/main/Image/3.Hiding.gif)
1. it make agent get hide from target one-time.
basically, this work is taking high cost. so i did some of trick for this. but cause of this, some of agent might get ignore that making new hiding spot.

2. for the making hiding position, there have obj near by. but obj should follow these structure
```csharp
bool IsObjAvailable(Collider collider, int layerMask)
{
    return !collider.isTrigger &&
            (collider.gameObject.isStatic ||
            (layerMask & 1 << transform.gameObject.layer) != 0);
}
```
3. when u look at the code
```csharp

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
```

as u can see, the hiding_StateModule basically doing nothing. in here only doing is, check can agent reach to position that get from navHideManager.

most things is happen to NavHideManager.


<details>
<summary>NavHideManager</summary>

1. the happen in NavHideManager is quite complicated. so in here, i will just explain simple way.

```csharp
public class NavHideManager : MonoBehaviour
{
    // the sphere size that will starting from target who request first time for hide in same target.
    [field: SerializeField] private float SphereCastRadius { set; get; } = 5f; 
    
    // how long hide data will cached?
    [field: SerializeField] private float MaxRefreshDistance { set; get; } = 1f;

    // cashing hide data by target.
    Dictionary<Transform, NavHideData> HideDataDic { set; get; } = new Dictionary<Transform, NavHideData>();

    // skip
}

```

2. the agent who first request that making hide data, will be overlap sphere with SphereCastRadius.

3. If the target moves within the MaxRefreshDistance from the point where the HideData was initially created, the HideData created at that time will be shared when another agent requests it.

<details>
<summary>How exactly works?</summary>

1. Find hideData with same target. if data dosent exist or target moved overthan MaxRefreshDistance, NavHideManager start to make new hideData.

```csharp
public class NavHideManager : MonoBehaviour
{
    public static NavHideManager Instance { set; get; }
    Dictionary<Transform, NavHideData> HideDataDic { set; get; } = new Dictionary<Transform, NavHideData>();
    public bool TryAddHide(Transform target, NavMeshAgent agent, out Vector3 hidePosition, int layerMask)
    {
        hidePosition = Vector3.zero;

        NavHideData hideData = null;
        if (HideDataDic.ContainsKey(target))
        {
            // if target moved more than limited dist from last Position. it will remove.
            if (HideDataDic[target].ContectedPosition.GetDistance(target.position) >= MaxRefreshDistance)
                HideDataDic.Remove(target);
            else
                hideData = HideDataDic[target];
        }

        if (hideData == null)
        {
            var paths = GetHidePosition(target.position, agent, layerMask);

            var dic = new Dictionary<Vector3, NavMeshAgent>();
            paths.ForEach(x =>
            {
                dic.Add(x, null);
            });
            hideData = new NavHideData(target.position, dic);
            HideDataDic.Add(target, hideData);
        }

        var canAdd = hideData.CanAddHidePosition(agent);
        if (canAdd)
        {
            hidePosition = hideData.AddAgent(agent);
        }

        return canAdd;
    }
}
```

2. when NavHideManager making new hideData, collecting colliders near by agent who first request.

3. after got colliders, navHideManager start to calc radius.
every collider has bouns.min or max from pivot. so navHideManager will get distance between pivot to min, max. most longest one will be radius of obj.

*why?
the longest radius can cover collider in every direction.

4. from here, it got little complicated.
cause obj shape will not exactly circle, we need remove empty distance between raidus to obj pivot. for this we need ray-cast. and also obj can touch end of raidus, so we have to consider that too. cause if ray-cast start in obj, ray-cast cant catch the obj.

for this, we will raycast from radius + calibration value. in navHideManager, calibration value is 1f. this will be total objRadius.

technically, what we are trying to do is, how many collider can make hide position from target. so we need radius from target too.

In conclusion, final radius will be radiusFromTarget = Vector3.Distance(collider.position, target.position) + objRadius; and direction will be (collider.position - target.position).normalize.

so. what if we found hide position end of "radiusFromTarget"? how we can find next position?

in this moment, at least we know next hide position should be far away from hide position that we just found. like it should be far away more than agent.radius * 2f. for this, we need get eachAngle for finding next hide Position. for this we need trigonometric functions.


```csharp
List<Vector3> GetHidePointFromColider(Collider collider, Vector3 target, Vector3 agentPosition, float agentRadius, float agentHeight, NavMeshQueryFilter filter, int layerMask)
{
    GizmosDrawerManager.Instance.DrawLine(agentPosition, collider.transform.position, 2f, debugingLineColor[debugingLine.ConnectAgentToCollider], ShouldDrawGizmos);
    var hidePositions = new List<Vector3>();
    var correctionDist = 1f;
    var objMaxPosition = collider.bounds.max.GetDistance(collider.transform.position) > collider.bounds.min.GetDistance(collider.transform.position) ? collider.bounds.max : collider.bounds.min;
    var objRadius = objMaxPosition.GetDistance(collider.transform.position) + correctionDist;
    var radiusFromTarget = collider.transform.position.GetOverrideY(0).GetDistance(target.GetOverrideY(0)) + objRadius;
    var agentHeightCenter = agentPosition + Vector3.up * agentHeight * 0.5f;
    var dirTargetToCollider = target.GetOverrideY(0).GetDirection(collider.transform.position.GetOverrideY(0));
    var basePoint = collider.transform.position.GetOverrideY(agentHeightCenter.y) + dirTargetToCollider * objRadius;
    var eachAngle = (float)Math.Asin(agentRadius * 2.5f / radiusFromTarget) * Mathf.Rad2Deg;
    var maxAngle = (float)Math.Asin(objRadius / radiusFromTarget) * Mathf.Rad2Deg;
    Func<bool, Vector3, float, float, Vector3> getSidePosition = (isLeft, centerPosition, angle, dist) => centerPosition + (Quaternion.Euler(0, (float)(isLeft ? -angle : angle), 0) * dirTargetToCollider) * dist;
    var isLeftEnd = false;
    var isRightEnd = false;

    var lastLeftHit = Vector3.zero;
    var lastRightHit = Vector3.zero;

    for (float angle = 0; angle < maxAngle && (!isLeftEnd || !isRightEnd); angle += (float)eachAngle)
    {
        var count = hidePositions.Count;
        for (int i = 0; i < 2; i++)
        {
            var isLeft = i == 0;
            if (isLeft && isLeftEnd) continue;
            else if (isRightEnd && isRightEnd) continue;

            var sidePoint = getSidePosition(isLeft, target.GetOverrideY(agentHeightCenter.y), angle, radiusFromTarget);
            GizmosDrawerManager.Instance.DrawLine(sidePoint, collider.transform.position.GetOverrideY(basePoint.y), 2f, debugingLineColor[debugingLine.ConnectColliderToHideSameplePosition], ShouldDrawGizmos);
            GizmosDrawerManager.Instance.DrawLine(sidePoint, target.GetOverrideY(agentHeightCenter.y), 2f, debugingLineColor[debugingLine.ConnectHideSameplePositionToTarget], ShouldDrawGizmos);

            if (CanHidePosition(angle == 0 ? collider : null, sidePoint, target, isLeft ? lastLeftHit : lastRightHit, objRadius, agentRadius, agentHeight, layerMask, filter, out Vector3 hidePosition))
            {
                hidePositions.Add(hidePosition);
                if (isLeft)
                    lastLeftHit = hidePosition;
                else
                    lastRightHit = hidePosition;
            }
            if (count == hidePositions.Count)
            {
                if (isLeft)
                    isLeftEnd = true;
                else
                    isRightEnd = true;
            }

            if (angle == 0f)
            {
                if (isLeftEnd)
                    isRightEnd = true;
                break;
            }
        }

    }

    return hidePositions;
}
```

from u found hide position, "NavHideManager" calc that agent is actually can hiding that position or not.(Agent height, radius etc.)

```csharp
// checking position with agent radius and height
bool CanHidePosition(Collider collider, Vector3 startPosition, Vector3 targetPosition, Vector3 lastHit, float mostFarFromCollider, float agentRadius, float agentHeight, int layerMask, NavMeshQueryFilter filter, out Vector3 hidePosition)
{
    hidePosition = Vector3.zero;
    var dirStartToTarget = startPosition.GetOverrideY(0).GetDirection(targetPosition.GetOverrideY(0));
    GizmosDrawerManager.Instance.DrawSphere(startPosition, 0.05f, 2f, debugingSpereColor[debugingSpere.HideSamplePosition], ShouldDrawGizmos);
    var hits =
        startPosition.GetOverrideY(startPosition.y).RayCastAll(targetPosition.GetOverrideY(startPosition.y))
            .Where(x => x.point.GetDistance(lastHit) > agentRadius * 2.5f && IsObjAvailable(x.collider, layerMask))
            .OrderBy(x => Vector3.Distance(x.point, startPosition)).ToList();

    // cause ground will be stay under Agent always,
    // after this, u can except floor collision.
    if (collider != null && !IsColliderExist(collider, hits)) return false;

    if (hits.Count == 0 || !IsCoveredByOtherObj(targetPosition, hits[0].point + -dirStartToTarget * agentRadius, layerMask)) return false;

    var mostClosedObj = hits[0].point + -dirStartToTarget * agentRadius;
    var floors = mostClosedObj.RayCastAll(Vector3.down, mostFarFromCollider);

    if (floors.Count == 0 || !IsCoveredByOtherObj(targetPosition, floors[0].point, layerMask)) return false;

    var floor = floors[0].point;
    var top = floor + Vector3.up * agentHeight;
    GizmosDrawerManager.Instance.DrawLine(floor, top, 2f, debugingLineColor[debugingLine.DrawLine], ShouldDrawGizmos);

    Func<bool, Vector3, Vector3> getSidePosition = (isLeft, centerPosition) => centerPosition + (Quaternion.Euler(0, isLeft ? -90 : 90, 0) * dirStartToTarget) * agentRadius;
    var basePosition = getSidePosition(true, top);
    GizmosDrawerManager.Instance.DrawLine(floor, basePosition, 2f, debugingLineColor[debugingLine.ConnectFloorToEachSide], ShouldDrawGizmos);
    if (IsCoveredByOtherObj(targetPosition, basePosition, layerMask))
    {
        var opponentPosition = getSidePosition(false, top);
        GizmosDrawerManager.Instance.DrawLine(floor, opponentPosition, 2f, debugingLineColor[debugingLine.ConnectFloorToEachSide], ShouldDrawGizmos);
        GizmosDrawerManager.Instance.DrawLine(basePosition, opponentPosition, 2f, debugingLineColor[debugingLine.ConnectSideToSide], ShouldDrawGizmos);
        var isBothHit = IsCoveredByOtherObj(targetPosition, opponentPosition, layerMask);
        if (isBothHit)
        {
            for (int i = 0; i < 5; i++)
            {
                var floorPosition = floor + -dirStartToTarget * i * agentRadius * 2.5f;
                if (NavMesh.SamplePosition(floorPosition, out NavMeshHit hit, agentRadius, filter))
                {
                    hidePosition = floorPosition;
                    return true;
                }
            }
        }
    }

    GizmosDrawerManager.Instance.DrawSphere(basePosition, 0.02f, 2f, debugingSpereColor[debugingSpere.IsntHit], ShouldDrawGizmos);
    return false;
}
```

5. after making hideData, who ever request hiding position, as long as target move under "MaxRefreshDistance" navHideManager keep sharing hideData to other agent.

```csharp
public class NavHideData
{
    public Vector3 ContectedPosition { private set; get; }
    public float AgentTypeID { private set; get; }
    public Dictionary<Vector3, NavMeshAgent> HidePositions { private set; get; } = new Dictionary<Vector3, NavMeshAgent>();
    public NavHideData(Vector3 lastContectedPosition, Dictionary<Vector3, NavMeshAgent> hidePositions)
    {
        ContectedPosition = lastContectedPosition;
        HidePositions = hidePositions;
    }
    public bool CanAddHidePosition(NavMeshAgent agent)
    {
        if (HidePositions.Count == 0) return false;
        if (agent.agentTypeID != AgentTypeID) return false;
        else if (!HidePositions.Values.Contains(null)) return false;
        else if (HidePositions.Values.Contains(agent)) return false;

        return true;
    }

    public Vector3 AddAgent(NavMeshAgent agent)
    {
        // in the first time. 
        for (int i = 0; i < 2; i++)
        {
            var positions = HidePositions.Keys.Where(x => (i == 0 ? Vector3.Dot(x, agent.transform.position) <= 0 : Vector3.Dot(x, agent.transform.position) > 0) && HidePositions[x] == null).OrderBy(x => x.GetDistance(agent.transform.position));
            foreach (var key in positions)
            {
                HidePositions[key] = agent;
                return key;
            }
        }

        return Vector3.zero;
    }
}
```

agent try to hide in the object behind the agent, but if it fails, it try to hide elsewhere, even if it passes through the target.

if there has no more position for hiding, hidingState will call-back that OnFailedToFindWay() function in moduleData.

</details>

</details>

</details>

<details>
<summary>Checking</summary>

1. checking state is working different with other state.

cause this state just for checking, it will not change state. 

```csharp

public void StartState(NavAgentModule.NavAgentModuleData moduleData, Action onDoneHandler)
{
    switch (moduleData.State)
    {
        case NavAgentModule.StateList.Pointing:
        case NavAgentModule.StateList.Tracking:
        case NavAgentModule.StateList.Hiding:
            OnDoneHandler = onDoneHandler;
            isInterrupting = StateModuleHandler.IsPlayingModuleRunning();
            StateModuleHandler.EnterModule((int)moduleData.State, moduleData);
            break;
        // checking module is works different cause it just for cheking that agent could reach to target or not.
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
```

2. also because its not working with StateModuleHandler, onDoneHandler function that call when state done, will not functioning.

for this. u have to use CheckingOption.

```csharp
public class NavAgentModuleData : StateModuleData
{
    public BasicOption BasicModulerSetting { private set; get; }
    public CheckingOption CheckingMoudlerSetting { private set; get; }

    public NavAgentModuleData(BasicOption basicOption, CheckingOption checkingOption)
    {
        BasicModulerSetting = basicOption;
        CheckingMoudlerSetting = checkingOption;
    }

    public class CheckingOption
    {
        public Action<bool> OnFindWay { private set; get; } = null;
        public CheckingOption(Action<bool> onFindWay)
        {
            OnFindWay = onFindWay;
        }
    }
}
```

3. in the checkingState, it will let u know agent can reached to target or not.

again. this is just for checking. so when this state working. state will not change.

</details>
</details>

------------------------
<details>
<summary>한국어</summary>

# 어떻게 설치하죠?

설치전, 해당 프로젝트는 다른 프로젝트에 디펜딩되어 있습니다. 각 버전마다 디펜딩된 프로젝트를 먼저 확인해주세요.  
각 버전 디펜던시는 package.json 파일을 통해 확인할 수 있습니다.  

이후 직접 다운로드해서 프로젝트의 Assets에 설치합니다.

# 이게 뭔가요?

FSM패턴으로 디자인된 NavMeshAgent를 관리하는 컴포넌트입니다.

# 어디에 쓸 수 있죠?

NavMeshAgent에 대한 상태별 관리가 미리정의된 클래스가 필요하다면 사용가능합니다.

# How to Use This?

1. 게임 오브젝트에 "NavAgentHandler" 컴포넌트를 추가합니다.
* NavAgentHandler가 설치되는 GameObject에는 "NavMeshAgent"와 "NavMeshObstacle"이 자동으로 추가됩니다.
```csharp
namespace LumenCat92.Nav
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(NavMeshObstacle))]
    public class NavAgentHandler : MonoBehaviour
    {
        //skip
    }
    //skip
}
```

2. Manager Folder 안의 모든 매니져 스크립트를 다른 게임 오브젝트에 추가합니다.

3. NavAgentHandler 를 보면,

```csharp
public class NavAgentHandler : MonoBehaviour
{    
    // skip
    
    [field: SerializeField] public bool AllowedChekingDeadLock { set; get; } = true;
    [field: SerializeField] public LayerMask HideableLayer { private set; get; }
    private Action OnDoneHandler { set; get; }
    
    // skip
    
    // 이것은 navAgentHandler 시작부분입니다.
    // 안전성을 위해, 처리에 대해서 접근하는 것을 허용하지 않습니다.
    // 각 스테이트가 끝났다면. "onDoneHandler"를 실행합니다.
    public void StartState(NavAgentModule.NavAgentModuleData moduleData, Action onDoneHandler)
    {
        switch (moduleData.State)
        {
            case NavAgentModule.StateList.Pointing:
            case NavAgentModule.StateList.Tracking:
            case NavAgentModule.StateList.Hiding:
                OnDoneHandler = onDoneHandler;
                isInterrupting = StateModuleHandler.IsPlayingModuleRunning();
                StateModuleHandler.EnterModule((int)moduleData.State, moduleData);
                break;
            
            // checking 스테이트는 단순 체킹 용도임으로, 다른 스테이트들과 다르게 동작합니다.
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

    // 만약 스테이트가 작동중에 다른 스테이트로 전환을 시도한다면,
    // 기존 onDoneHandler는 무시됩니다.
    public void DoWhenStateDone()
    {
        if (!isInterrupting)
        {
            OnDoneHandler?.Invoke();
        }
    }
}

```

보시는 것처럼, NavAgentModule.NavAgentModuleData.StateList안의 스테이트를 전환하려고 한다면

StartState()를 다시 실행해야합니다. 이떄, navAgentHandler는 기존 처리를 덮어씁니다.

또한, 처리를 덮어 쓸 경우, 이전에 등록했던 call-back 함수를 실행하지 않습니다.

한가지 확실하게 할 점은, Agent가 목적지까지의 도달 할 수 없을 경우, 이 또한 이전에 등록했던 call-back 함수를 실행하지 않습니다. 단, 이때는 NavAgentModuleData의 다른 함수를 실행합니다. 2번을 확인해주세요. 

2. 사용을 위해 NavAgentModule.NavAgentModuleData를 보면,

```csharp
public abstract partial class NavAgentModule
{
    public enum StateList
    {
        Pointing, // 목표 지점을 한번만 설정합니다.
        Tracking, // 목표에 도달 할때까지 계속 목표 지점을 설정합니다.
        Hiding, // 목표로부터 숨습니다.
        Cheking, // 목표까지 도달 가능 여부를 체크합니다. agent는 움직이지 않습니다.
        Non,
    }
    public class NavAgentModuleData : StateModuleData
    {
        public BasicOption BasicModulerSetting { private set; get; }
        public PointingOption PointingModulerSetting { private set; get; }
        public CheckingOption CheckingMoudlerSetting { private set; get; }
        public TrackingOption TrackingModulerSetting { private set; get; }
        public HidingOption HidingMoudlerSetting { private set; get; }
        public NavAgentModuleData(BasicOption basicOption, PointingOption pointingOption)
        {
            BasicModulerSetting = basicOption;
            PointingModulerSetting = pointingOption;
        }
        public NavAgentModuleData(BasicOption basicOption, TrackingOption trackingOption)
        {
            BasicModulerSetting = basicOption;
            TrackingModulerSetting = trackingOption;
        }
        public NavAgentModuleData(BasicOption basicOption, HidingOption hidingOption)
        {
            BasicModulerSetting = basicOption;
            HidingMoudlerSetting = hidingOption;
        }
        public NavAgentModuleData(BasicOption basicOption, CheckingOption checkingOption)
        {
            BasicModulerSetting = basicOption;
            CheckingMoudlerSetting = checkingOption;
        }
        
        public class BasicOption
        {
            public Transform Target { private set; get; } // 목표지잠
            public bool ShouldSequenceProcessing { private set; get; } = true; // module 진행을 비동기로 진행합니다.
            public bool ShouldRotateManually { private set; get; } // *1) agent가 움직이는 동안 회전을 끕니다. 
            public float StopDist { private set; get; } // *2) NavMeshAgent.StoppingDistance와 동일합니다. 그러나 각 스테이트들의 끝나는 것에도 영향을 미칩니다.
            public bool ShouldActiveDeadLockCheck { private set; get; } // *3) 다중 에이전트간의 고착문제에 사용됩니다. 
            public Action OnFailedToFindWay { private set; get; } // 목표지점까지 도달 할 수 없을 경우 해당 함수를 통해 알립니다.

            public BasicOption(Transform target, bool shouldSequenceProcessing, bool isLookAtOn, float stopDist, bool shouldActiveDeadLockCheck, Action doWhenCantFindWay)
            {
                Target = target;
                ShouldSequenceProcessing = shouldSequenceProcessing;
                ShouldRotateManually = isLookAtOn;
                StopDist = stopDist;

                ShouldActiveDeadLockCheck = shouldActiveDeadLockCheck;
                OnFailedToFindWay = doWhenCantFindWay;
            }
        }
    }
}
```

    *1 에이전트가 걷고 있을 때, 오브젝트의 회전은 에이전트에 의존됩니다.

하지만 프로그래밍할 때, 오브젝트를 바라보게 하거나 목표물을 조준하게 만들기 위해 수동으로 회전을 제어하길 원할 수 있습니다.

이런 경우에, 이 옵션을 켤 수 있습니다. 단, 알아야 할 점은 회전은 수동으로 제어해야 한다는 것입니다. NavAgentHandler는 목표물을 바라보도록 지원하지 않습니다(이는 상황마다 다르기 때문).

    *2 StopDist 는 navMeshAgent.StoppingDisctance를 설정합니다. 그리고 마지막 목적지에도 영향을 줍니다. 

NavAgentModule에서, 다음과 같은 내용을 볼 수 있습니다.
```csharp
public abstract partial class NavAgentModule : StateModule
{
    public enum DistStateList { Reached, Near, Close, MiddleWay }

    //skip

    protected DistStateList GetStateByDist
    {
        get
        {
            var navAgent = StateModuleHandler.navMeshAgent;
            if (navAgent.pathPending) return DistStateList.MiddleWay;
            var standingPosition = navAgent.transform.position.GetOverrideY(0);
            var destinationPosition = navAgent.destination.GetOverrideY(0);

            // dist는 각 스테이트 모듈이 끝나는 것을 결정합니다.
            // 그리고 여기서 여러분이 지정한 StopDist가 사용됨을 확인 할 수 있습니다.
            var dist = standingPosition.GetDistance(destinationPosition) - ModuleData.BasicModulerSetting.StopDist;
            if (dist <= GetDistByState(DistStateList.Close))
            {
                // is destination placed higher or lower than agent?
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

    //skip
}
```

그러니 기술적으로, navMeshAgent.StoppingDistance 는 마지막 도착점에 영향을 미칩니다. 

    *3 두 에이전트가 좁은 통로에서 서로를 지나려 한다고 가정해봅시다.

이 상황에서는 두 에이전트가 서로에게 막혀 각자의 목적지에 도달하지 못할 수 있습니다.

이를 해결하기 위해 BasicModulerSetting에서 ShouldActiveDeadLockCheck 속성을 켤 수 있습니다.

코드를 보면,

```csharp

public abstract partial class NavAgentModule : StateModule
{
    //skip

    // enter module
    protected override void OnEnterModule()
    {
        Agent.updateRotation = ModuleData.BasicModulerSetting.ShouldRotateManually ? false : true;
        Agent.stoppingDistance = ModuleData.BasicModulerSetting.StopDist;
        Agent.speed = ModuleData.BasicModulerSetting.AgentSpeed;
        Agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        Agent.avoidancePriority = 10;
        var needNavi = Vector3.Distance(ModuleData.BasicModulerSetting.Target.position, Agent.transform.position) > eachStateDist[(int)DistStateList.Reached].Value;
        if (needNavi)
        {
            //checking deadLock
            if (ModuleData.BasicModulerSetting.ShouldActiveDeadLockCheck)
                CheckingDeadLock();

            ProcessingState = StateModuleHandler.OnStartCoroutine(StartNav(ModuleData, base.Exit));
        }
        else
        {
            base.Exit();
        }
    }

    protected void CheckingDeadLock()
    {
        var eachTime = 0.1f;
        var lastAgentPosition = Agent.transform.position;
        var speedCorrection = 0.8f;
        var expectingDist = Agent.transform.position.GetDistance(Agent.transform.position + Agent.velocity * eachTime * speedCorrection);
        deadLockTimeData = TimeCounterManager.Instance.SetTimeCounting(
            maxTime: eachTime,
            function: () =>
                {
                    if (GetStateByDist == DistStateList.MiddleWay)
                    {
                        if (Agent.obstacleAvoidanceType == ObstacleAvoidanceType.NoObstacleAvoidance)
                        {
                            var hits = Agent.transform.GetOverlapSphere(GetAgentRadius).Where(x => !x.gameObject.isStatic && !x.transform.IsChildOf(Agent.transform)).ToList();
                            if (hits.Count == 0)
                                Agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                        }
                        else
                        {
                            var dist = Agent.transform.position.GetDistance(lastAgentPosition);
                            if (dist < expectingDist)
                                Agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
                        }
                    }

                    deadLockTimeData = null;
                    if (ModuleData != null) CheckingDeadLock();
                },
            sequenceKey: ModuleData,
            sequnceMatch: (addedSeqeunce) => addedSeqeunce.Equals(ModuleData));
    }
}
```

navAgentModule은 일정 시간마다 예상 위치를 계산합니다. 

에이전트가 예상 위치에 도달하지 못하면, 주변을 확인한 후 모듈은 에이전트가 서로 다른 에이전트를 찾지 못할 때까지 충돌하지 않도록 만듭니다.

이는 에이전트가 많을수록 많은 겹침 현상이 발생할 수 있습니다. 해당 부분을 주의해주세요.

# NavAgentModule

다음은 각 스테이트에 대한 설명입니다.

<details>
<summary>Pointing</summary>

![1.Pointing.gif](https://github.com/lumenCat92/NavAgentHandler/blob/main/Image/1.Pointing.gif)

1. Pointing state는 기본 추적 상태 모듈입니다.

이 모듈은 목적지를 한 번만 설정합니다.

모듈 실행 중에 대상 위치가 변경되더라도, 변경된 위치는 목적지에 반영되지 않습니다.

```csharp
public abstract partial class NavAgentModule
{
    public enum StateList
    {
        Pointing,
        Tracking,
        Hiding,
        Cheking,
        Non,
    }
    public class NavAgentModuleData : StateModuleData
    {
        public BasicOption BasicMudulerSetting { private set; get; }
        public PointingOpition PointModulerSetting { private set; get; } = new PointingOpition();

        public NavAgentModuleData(BasicOption basicOption, PointingOption pointingOption)
        {
            BasicModulerSetting = basicOption;
            PointingModulerSetting = pointingOption;
        }
        
        public class BasicOption
        {
            public Transform Target { private set; get; } // 목적지점
            public bool ShouldSequenceProcessing { private set; get; } = true; // module 진행을 비동기로 진행합니다.
            public bool ShouldRotateManually { private set; get; } // agent가 움직이는 동안 회전을 끕니다.  
            public float StopDist { private set; get; } // NavMeshAgent.StoppingDistance와 동일합니다. 그러나 각 스테이트들의 끝나는 것에도 영향을 미칩니다.
            public bool ShouldActiveDeadLockCheck { private set; get; } // 다중 에이전트간의 고착문제에 사용됩니다. 
            public Action OnFailedToFindWay { private set; get; } // 목표지점까지 도달 할 수 없을 경우 해당 함수를 통해 알립니다.

            public BasicOption(Transform target, bool shouldSequenceProcessing, bool isLookAtOn, float stopDist, bool shouldActiveDeadLockCheck, Action doWhenCantFindWay)
            {
                Target = target;
                ShouldSequenceProcessing = shouldSequenceProcessing;
                ShouldRotateManually = isLookAtOn;
                StopDist = stopDist;

                ShouldActiveDeadLockCheck = shouldActiveDeadLockCheck;
                OnFailedToFindWay = doWhenCantFindWay;
            }
        }

        public class PointingOpition
        {
            public float StayingTime { private set; get; } = -1f; // 지점에서 얼마나 오래 있을건가요?
            public bool IsUnlimitedStayTime => StayingTime <= -1;  
            public bool CanPositionChange { private set; get; } = true; // 해당 지점을 다른 agent에게 양보할 수 있나요? 
            public TrafficDataSet TrafficDataSetting { private set; get; } = new TrafficDataSet(); // traffic 문제를 해결하기 위한 내용
            public PointingOpition() { }
            public PointingOpition(float stayingTime, bool canPositionChange, TrafficDataSet trafficDataSet = null)
            {
                StayingTime = stayingTime;
                CanPositionChange = canPositionChange;
                TrafficDataSetting = trafficDataSet == null ? this.TrafficDataSetting : trafficDataSet;
            }
            public class TrafficDataSet // 목표지점에 먼저 도착한 Agent가 해당 지점의 교통 세부사항에 대한 지휘를 획득합니다. 
            {
                public int MaxWaitingCount { private set; get; } = 3; // 얼마나 많은 agent가 해당 지점에 대기할 수 있나요?
                public float MaxStayingTime { private set; get; } = 3f; // 얼마나 오래 해당 지점에 대기할 수 있나요?
                public TrafficDataSet() { }
                public TrafficDataSet(int maxAddingCount, float maxStayingTime)
                {
                    MaxWaitingCount = maxAddingCount;
                    MaxStayingTime = maxStayingTime;
                }
            }
        }
    }
}
```

2. 여러 에이전트가 같은 위치에 도달하려고 한다면 어떻게 될까요?

이를 위해 TrafficDataSet과 NavTrafficManager가 필요합니다.


```csharp
public class NavTrafficManager : MonoBehaviour
{
    //skip

    IEnumerator HandlingTraffic(TrafficData.NavAgentModuleInfoData infoData, TrafficData trafficData)
    {
        if (infoData == null)
        {
            removeTrafficDataList.Add(trafficData);
            yield break;
        }
        else
        {
            if (!infoData.IsModuleRunning()) { StartCoroutine(HandlingTraffic(trafficData.ReadNextInfo(), trafficData)); yield break; }
            infoData.Agent.stoppingDistance = infoData.stoppingDistance;
            infoData.Agent.avoidancePriority = 0;
            float checkingTime = 0.1f;

            for (bool isModuleRunning = infoData.IsModuleRunning(); isModuleRunning; isModuleRunning = infoData.IsModuleRunning())
            {
                yield return new WaitForSeconds(checkingTime);
            }

            for (float dist = trafficData.Position.GetDistance(infoData.Agent.transform.position); dist < 0.5f; dist = trafficData.Position.GetDistance(infoData.Agent.transform.position))
            {
                yield return new WaitForSeconds(checkingTime);
            }

            StartCoroutine(HandlingTraffic(trafficData.ReadNextInfo(), trafficData));
        }
    }

    // skip

    public class TrafficData
    {
        public bool IsNew { private set; get; } = true; 
        public int MaxAddingCount { private set; get; } = 3;
        public float AllowedStayingTime { private set; get; } = 3f;
        public bool IsLimitedStayingTime { get => AllowedStayingTime != -1; }
        private List<NavAgentModuleInfoData> InfoList { set; get; } = new List<NavAgentModuleInfoData>();
        public NavAgentModuleInfoData ReadNextInfo()
        {
            IsNew = false;
            if (InfoList.Count > 0)
            {
                var data = InfoList[0];
                InfoList.RemoveAt(0);
                return data;
            }

            return null;
        }
        public Vector3 Position { private set; get; } = Vector3.zero;
        private int AddingCount { set; get; } = 0;
        
        //skip
    }
}
```

3. 하지만, 만약 Agent가 기다릴 필요가 없다면 어떻게 될까요? 예를 들어, Agent가 단순히 서 있거나, 도착한 후 다른 오브젝트에 의존하지 않는 애니메이션을 실행하려는 경우에 말이죠.

아니면, Agent가 반드시 그 위치에 서 있어야 하는 경우는 어떨까요? 예를 들어, Agent가 의자에 앉으려고 하거나, 도착한 후 다른 오브젝트에 의존하는 애니메이션을 실행하려는 경우 말입니다.

코드를 보면,

```csharp

public class PointingState_NavStateModule : NavAgentModule
{
    // skip

    protected override IEnumerator OnStartNav(NavAgentModuleData data)
    {
        // skip

        var state = GetStateByDist;
        while (state != DistStateList.Reached)
        {
            if (state == DistStateList.Close || state == DistStateList.Near)
            {
                state = GetStateByDist;
                // 만약 agent가 기다릴필요가 없다면? == Agent가 목표지점 변경을 허용한다면?
                if (data.PointingModulerSetting.CanPositionChange) 
                {
                    if (state == DistStateList.Near)
                        break;

                    var rayStartPoint = Agent.transform.position + Vector3.up * GetAgentHeight * 0.5f;
                    var rayDir = rayStartPoint.GetDirection(ModuleData.BasicModulerSetting.Target.position);
                    OnDrawGizmosSphere(rayStartPoint, 0.02f, 2f, Color.cyan);
                    OnDrawGizmosLine(rayStartPoint, rayDir, 2f, 2f, Color.black);

                    // 목표지점에 최대한 가깝게 접근하려 합니다. 이는 목표지점이 될 것 입니다.
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
                    }
                }

                // 만약 agent가 기다려야 한다면? == Agent가 목표지점 변경을 허용하지 않는다면?
                else
                {
                    // 트레픽 데이터를 추가합니다.
                    // 이미 교통 체증이 있는 상황인가요?
                    if (!NavTrafficManager.Instance.TryAddTrafficNearBy(
                            agent: Agent,
                            isModuleRunning: () => { return IsModuleRunning; },
                            maxAddingCount: ModuleData.PointingModulerSetting.TrafficDataSetting.MaxWaitingCount,
                            allowedStayingTime: ModuleData.PointingModulerSetting.TrafficDataSetting.MaxStayingTime,
                            stayingTime: ModuleData.PointingModulerSetting.StayingTime,
                            position: correctedPosition,
                            castRadius: GetAgentRadius))
                    {
                        ModuleData.BasicModulerSetting.OnFailedToFindWay?.Invoke();
                        break;
                    }
                }
            }
        }
    }
}
```
트레픽과 데드락이 적용된 경우.
 ![1.Pointing_TrafficAndDeadLock.gif](https://github.com/lumenCat92/NavAgentHandler/blob/main/Image/1.Pointing_TrafficAndDeadLock.gif)

목적지 변경을 허용하면, 에이전트는 가능한 한 목표에 가까이 가려고 시도합니다. 하지만 설정한 목적지에 도달하는 것은 보장되지 않습니다.

목적지 변경을 허용하지 않으면, 에이전트는 목적지의 교통 상황을 확인하려고 합니다. 만약 교통이 매우 혼잡하다면, 이는 기술적으로 목적지 탐색에 실패했음을 의미합니다. 이때 설정한 콜백 함수가 호출됩니다.

기억하셔야 할 점은, 이 과정은 에이전트가 목표에 가까이 갔을 때 발생합니다.

</details>

<details>
<summary>Tracking</summary>

 ![2.Tracking.gif](https://github.com/lumenCat92/NavAgentHandler/blob/main/Image/2.Tracking.gif)

1. pointing 스테이트와 가장 다른 점이라고 한다면, tracking 스테이트에서는, 타겟의 위치에 따라 목표지점을 계속해서 업데이트 한다는 겁니다.

```csharp
public class TrackingState_NavStateModule : NavAgentModule
{
    // skip
    protected override IEnumerator OnStartNav(NavAgentModuleData data)
    {
        var hasBeenCheckNearBy = false;

        var lastPosition = data.BasicModulerSetting.Target.position;
        if (!TrySetDestination(lastPosition, ref correctedPosition))
        {
            data.BasicModulerSetting.OnFailedToFindWay?.Invoke();
            yield break;
        }

        // IsNonStopTracking 옵션에 따라서, 반복 정도가 결정됩니다.
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

            // 목표지점과 가까워졌을시 목표지점을 양보할 수 있나?
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
```

2. tracking 스테이트는  traffic mananger를 사용하지 않기에, 지점에서 기다린다던가 와 같은 내용은 지원하지 않습니다. 또한 한 지점에 너무 많은 에이전트가 몰리가 된다면, 충돌 문제로 인해 목표지점에 도달 하지 못할 수 도 있습니다. 이를 위해 deadLock을 킬 수는 있으나, 에이전트가 겹치는 문제가 있을 수 있으니 상황에 맞게 사용하기 바랍니다.

3. pointing 스테이트처럼, 목표지점에 도달하면 스테이트가 끝나지만, 설정을 통해 도착 여부와 관계없이 지속적으로 목표지점을 따라가도록 설정할 수도 있습니다.

```csharp

public abstract partial class NavAgentModule
{
    public class NavAgentModuleData : StateModuleData
    {
        public BasicOption BasicModulerSetting { private set; get; }
        public TrackingOption TrackingModulerSetting { private set; get; }
        public NavAgentModuleData(BasicOption basicOption, TrackingOption trackingOption)
        {
            BasicModulerSetting = basicOption;
            TrackingModulerSetting = trackingOption;
        }

        public class BasicOption
        {
            public Transform Target { private set; get; }
            public bool ShouldSequenceProcessing { private set; get; } = true;
            public bool ShouldRotateManually { private set; get; }
            public float StopDist { private set; get; }
            public float AgentSpeed { private set; get; }
            public bool ShouldActiveDeadLockCheck { private set; get; }
            public Action OnFailedToFindWay { private set; get; }

            public BasicOption(StateList targetState, Transform target, bool isLookAtOn, float stopDist, float agentSpeed, bool shouldActiveDeadLockCheck, Action doWhenCantFindWay)
            {
                TargetState = targetState;
                Target = target;
                IsLookAtOn = isLookAtOn;
                StopDist = stopDist;
                AgentSpeed = agentSpeed;
                ShouldActiveDeadLockCheck = shouldActiveDeadLockCheck;
                OnFailedToFindWay = doWhenCantFindWay; 
            }
        }

        public class TrackingOption
        {
            public bool IsNonStopTracking { private set; get; }
            public TrackingOption(bool isNonStopTracking)
            {
                IsNonStopTracking = isNonStopTracking;
            }
        }
    }
}
```

</details>

<details>
<summary>Hiding</summary>

 ![3.Hiding.gif](https://github.com/lumenCat92/NavAgentHandler/blob/main/Image/3.Hiding.gif)

1. 목표로부터 한번만 숨는것을 설정합니다.

기본적으로, 이 작업은 많은 비용을 발생시킵니다. 이를 위해 몇가지 트릭이 사용되었습니다만, 이로 인해 몇몇 에이전트의 새로운 숨는 지점 만들기는 무시될 수 있습니다.

2. 숨는 지점을 만들기 위해서는 대상 오브젝트들은 다음과 같은 구조를 따라야 합니다.
```csharp
bool IsObjAvailable(Collider collider, int layerMask)
{
    return !collider.isTrigger &&
            (collider.gameObject.isStatic ||
            (layerMask & 1 << transform.gameObject.layer) != 0);
}
```
3. 코드를 보시면
```csharp

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
```

보는 것처럼, hiding 스테이트는 기본적으로 별다른 동작을 하지 않습니다. 여기서 하는 거라곤 navHideManager로부터 받은 숨는 지점에 실제로 도달 하게끔 만들 뿐입니다.

대부분의 일은 NavHideManager에서 일어납니다.


<details>
<summary>NavHideManager</summary>

1. NavHideManager 에서 일어나는 일은 좀 복잡한 편입니다. 가급적이면 간단하게 설명하겠습니다.

```csharp
public class NavHideManager : MonoBehaviour
{
    // 첫 번째 요정한 에이전트로부터 발생할 구체의 크기.
    [field: SerializeField] private float SphereCastRadius { set; get; } = 5f; 
    
    // 얼마나 오래 hideData를 보관할 것인가?
    [field: SerializeField] private float MaxRefreshDistance { set; get; } = 1f;

    // 목표에 의한 케싱된 hideData.
    Dictionary<Transform, NavHideData> HideDataDic { set; get; } = new Dictionary<Transform, NavHideData>();

    // skip
}

```

2. HideData를 만들기 위해 첫 번째로 요청한 에이전트를 중심으로 SphereCastRadius를 호출합니다.

3. 목표가 HideData를 만든 최초의 지점으로부터 MaxRefreshDistance 아래로 움직인다면, 이때 만들어진 hideData는 다른 Agent가 요청할 때, 공유됩니다.

<details>
<summary>정확히 어떨게 작동하는 건가요?</summary>

1. 같은 목표를 가진 hideData를 찾습니다. 데이터가 존재하지 않거나 목표가 MaxRefreshDistance를 초과하여 이동한 경우, NavHideManager는 새로운 hideData를 생성하기 시작합니다.

```csharp
public class NavHideManager : MonoBehaviour
{
    public static NavHideManager Instance { set; get; }
    Dictionary<Transform, NavHideData> HideDataDic { set; get; } = new Dictionary<Transform, NavHideData>();
    public bool TryAddHide(Transform target, NavMeshAgent agent, out Vector3 hidePosition, int layerMask)
    {
        hidePosition = Vector3.zero;

        NavHideData hideData = null;
        if (HideDataDic.ContainsKey(target))
        {
            // if target moved more than limited dist from last Position. it will remove.
            if (HideDataDic[target].ContectedPosition.GetDistance(target.position) >= MaxRefreshDistance)
                HideDataDic.Remove(target);
            else
                hideData = HideDataDic[target];
        }

        if (hideData == null)
        {
            var paths = GetHidePosition(target.position, agent, layerMask);

            var dic = new Dictionary<Vector3, NavMeshAgent>();
            paths.ForEach(x =>
            {
                dic.Add(x, null);
            });
            hideData = new NavHideData(target.position, dic);
            HideDataDic.Add(target, hideData);
        }

        var canAdd = hideData.CanAddHidePosition(agent);
        if (canAdd)
        {
            hidePosition = hideData.AddAgent(agent);
        }

        return canAdd;
    }
}
```

2. NavHideManager가 새로운 hideData를 생성할 때, 첫 번째 요청을 한 에이전트 근처의 콜라이더를 수집합니다.

3. 콜라이더를 수집한 후, navHideManager는 반지름을 계산하기 시작합니다. 각 콜라이더는 축으로부터 bounds.min 또는 max를 가지고 있습니다. 그래서 navHideManager는 축부터 min, max까지의 거리를 구합니다. 가장 긴 거리가 객체의 반지름이 됩니다.

*이유는?
가장 긴 반지름이 모든 방향에서 콜라이더를 커버할 수 있기 때문입니다.

4. 여기부터는 조금 복잡해집니다.

객체의 모양이 정확히 원형이 아니기 때문에, 반지름과 객체 중심 사이의 빈 거리를 제거해야 합니다. 이를 위해 레이캐스트가 필요합니다. 또한 객체가 반지름 끝에 닿을 수 있기 때문에, 이것도 고려해야 합니다. 왜냐하면 레이캐스트가 객체 내에서 시작하면 객체를 감지할 수 없기 때문입니다.

이를 위해 반지름 + 보정값에서 레이캐스트를 시작합니다. navHideManager에서 보정값은 1f입니다. 이것이 전체 objRadius가 됩니다.

기술적으로, 우리가 하려는 것은 하나의 콜라이더가 목표물로부터 얼마나 많은 숨는 위치를 만들 수 있는지를 알아내는 것입니다. 그래서 목표물로부터의 반지름도 필요합니다.

결론적으로, 최종 반지름은 radiusFromTarget = Vector3.Distance(collider.position, target.position) + objRadius가 되고, 방향은 (collider.position - target.position).normalize가 됩니다.

그렇다면 "radiusFromTarget" 끝에서 숨는 위치를 찾으면 어떻게 될까요? 다음 위치를 어떻게 찾을 수 있을까요?

이 순간, 적어도 우리는 다음 숨는 위치가 방금 찾은 숨는 위치에서 멀리 떨어져 있어야 한다는 것을 알고 있습니다. 이는 에이전트의 반지름 * 2f보다 더 멀리 떨어져 있어야 합니다. 이를 위해, 다음 숨는 위치를 찾기 위해 각도를 구해야 합니다. 이를 위해 삼각함수가 필요합니다.


```csharp
List<Vector3> GetHidePointFromColider(Collider collider, Vector3 target, Vector3 agentPosition, float agentRadius, float agentHeight, NavMeshQueryFilter filter, int layerMask)
{
    GizmosDrawerManager.Instance.DrawLine(agentPosition, collider.transform.position, 2f, debugingLineColor[debugingLine.ConnectAgentToCollider], ShouldDrawGizmos);
    var hidePositions = new List<Vector3>();
    var correctionDist = 1f;
    var objMaxPosition = collider.bounds.max.GetDistance(collider.transform.position) > collider.bounds.min.GetDistance(collider.transform.position) ? collider.bounds.max : collider.bounds.min;
    var objRadius = objMaxPosition.GetDistance(collider.transform.position) + correctionDist;
    var radiusFromTarget = collider.transform.position.GetOverrideY(0).GetDistance(target.GetOverrideY(0)) + objRadius;
    var agentHeightCenter = agentPosition + Vector3.up * agentHeight * 0.5f;
    var dirTargetToCollider = target.GetOverrideY(0).GetDirection(collider.transform.position.GetOverrideY(0));
    var basePoint = collider.transform.position.GetOverrideY(agentHeightCenter.y) + dirTargetToCollider * objRadius;
    var eachAngle = (float)Math.Asin(agentRadius * 2.5f / radiusFromTarget) * Mathf.Rad2Deg;
    var maxAngle = (float)Math.Asin(objRadius / radiusFromTarget) * Mathf.Rad2Deg;
    Func<bool, Vector3, float, float, Vector3> getSidePosition = (isLeft, centerPosition, angle, dist) => centerPosition + (Quaternion.Euler(0, (float)(isLeft ? -angle : angle), 0) * dirTargetToCollider) * dist;
    var isLeftEnd = false;
    var isRightEnd = false;

    var lastLeftHit = Vector3.zero;
    var lastRightHit = Vector3.zero;

    for (float angle = 0; angle < maxAngle && (!isLeftEnd || !isRightEnd); angle += (float)eachAngle)
    {
        var count = hidePositions.Count;
        for (int i = 0; i < 2; i++)
        {
            var isLeft = i == 0;
            if (isLeft && isLeftEnd) continue;
            else if (isRightEnd && isRightEnd) continue;

            var sidePoint = getSidePosition(isLeft, target.GetOverrideY(agentHeightCenter.y), angle, radiusFromTarget);
            GizmosDrawerManager.Instance.DrawLine(sidePoint, collider.transform.position.GetOverrideY(basePoint.y), 2f, debugingLineColor[debugingLine.ConnectColliderToHideSameplePosition], ShouldDrawGizmos);
            GizmosDrawerManager.Instance.DrawLine(sidePoint, target.GetOverrideY(agentHeightCenter.y), 2f, debugingLineColor[debugingLine.ConnectHideSameplePositionToTarget], ShouldDrawGizmos);

            if (CanHidePosition(angle == 0 ? collider : null, sidePoint, target, isLeft ? lastLeftHit : lastRightHit, objRadius, agentRadius, agentHeight, layerMask, filter, out Vector3 hidePosition))
            {
                hidePositions.Add(hidePosition);
                if (isLeft)
                    lastLeftHit = hidePosition;
                else
                    lastRightHit = hidePosition;
            }
            if (count == hidePositions.Count)
            {
                if (isLeft)
                    isLeftEnd = true;
                else
                    isRightEnd = true;
            }

            if (angle == 0f)
            {
                if (isLeftEnd)
                    isRightEnd = true;
                break;
            }
        }

    }

    return hidePositions;
}
```

숨는 위치에서 "NavHideManager"는 에이전트가 실제로 해당 위치를 숨는 수 있는지 여부를 계산합니다.(Agent 높이, 너비 등)

```csharp
// checking position with agent radius and height
bool CanHidePosition(Collider collider, Vector3 startPosition, Vector3 targetPosition, Vector3 lastHit, float mostFarFromCollider, float agentRadius, float agentHeight, int layerMask, NavMeshQueryFilter filter, out Vector3 hidePosition)
{
    hidePosition = Vector3.zero;
    var dirStartToTarget = startPosition.GetOverrideY(0).GetDirection(targetPosition.GetOverrideY(0));
    GizmosDrawerManager.Instance.DrawSphere(startPosition, 0.05f, 2f, debugingSpereColor[debugingSpere.HideSamplePosition], ShouldDrawGizmos);
    var hits =
        startPosition.GetOverrideY(startPosition.y).RayCastAll(targetPosition.GetOverrideY(startPosition.y))
            .Where(x => x.point.GetDistance(lastHit) > agentRadius * 2.5f && IsObjAvailable(x.collider, layerMask))
            .OrderBy(x => Vector3.Distance(x.point, startPosition)).ToList();

    // cause ground will be stay under Agent always,
    // after this, u can except floor collision.
    if (collider != null && !IsColliderExist(collider, hits)) return false;

    if (hits.Count == 0 || !IsCoveredByOtherObj(targetPosition, hits[0].point + -dirStartToTarget * agentRadius, layerMask)) return false;

    var mostClosedObj = hits[0].point + -dirStartToTarget * agentRadius;
    var floors = mostClosedObj.RayCastAll(Vector3.down, mostFarFromCollider);

    if (floors.Count == 0 || !IsCoveredByOtherObj(targetPosition, floors[0].point, layerMask)) return false;

    var floor = floors[0].point;
    var top = floor + Vector3.up * agentHeight;
    GizmosDrawerManager.Instance.DrawLine(floor, top, 2f, debugingLineColor[debugingLine.DrawLine], ShouldDrawGizmos);

    Func<bool, Vector3, Vector3> getSidePosition = (isLeft, centerPosition) => centerPosition + (Quaternion.Euler(0, isLeft ? -90 : 90, 0) * dirStartToTarget) * agentRadius;
    var basePosition = getSidePosition(true, top);
    GizmosDrawerManager.Instance.DrawLine(floor, basePosition, 2f, debugingLineColor[debugingLine.ConnectFloorToEachSide], ShouldDrawGizmos);
    if (IsCoveredByOtherObj(targetPosition, basePosition, layerMask))
    {
        var opponentPosition = getSidePosition(false, top);
        GizmosDrawerManager.Instance.DrawLine(floor, opponentPosition, 2f, debugingLineColor[debugingLine.ConnectFloorToEachSide], ShouldDrawGizmos);
        GizmosDrawerManager.Instance.DrawLine(basePosition, opponentPosition, 2f, debugingLineColor[debugingLine.ConnectSideToSide], ShouldDrawGizmos);
        var isBothHit = IsCoveredByOtherObj(targetPosition, opponentPosition, layerMask);
        if (isBothHit)
        {
            for (int i = 0; i < 5; i++)
            {
                var floorPosition = floor + -dirStartToTarget * i * agentRadius * 2.5f;
                if (NavMesh.SamplePosition(floorPosition, out NavMeshHit hit, agentRadius, filter))
                {
                    hidePosition = floorPosition;
                    return true;
                }
            }
        }
    }

    GizmosDrawerManager.Instance.DrawSphere(basePosition, 0.02f, 2f, debugingSpereColor[debugingSpere.IsntHit], ShouldDrawGizmos);
    return false;
}
```

5. hideData를 생성한 후, 목표가 "MaxRefreshDistance" 내에서 이동하는 한, 숨는 위치를 요청하는 누구에게나 navHideManager는 hideData를 다른 에이전트와 계속 공유합니다.

```csharp
public class NavHideData
{
    public Vector3 ContectedPosition { private set; get; }
    public float AgentTypeID { private set; get; }
    public Dictionary<Vector3, NavMeshAgent> HidePositions { private set; get; } = new Dictionary<Vector3, NavMeshAgent>();
    public NavHideData(Vector3 lastContectedPosition, Dictionary<Vector3, NavMeshAgent> hidePositions)
    {
        ContectedPosition = lastContectedPosition;
        HidePositions = hidePositions;
    }
    public bool CanAddHidePosition(NavMeshAgent agent)
    {
        if (HidePositions.Count == 0) return false;
        if (agent.agentTypeID != AgentTypeID) return false;
        else if (!HidePositions.Values.Contains(null)) return false;
        else if (HidePositions.Values.Contains(agent)) return false;

        return true;
    }

    public Vector3 AddAgent(NavMeshAgent agent)
    {
        // in the first time. 
        for (int i = 0; i < 2; i++)
        {
            var positions = HidePositions.Keys.Where(x => (i == 0 ? Vector3.Dot(x, agent.transform.position) <= 0 : Vector3.Dot(x, agent.transform.position) > 0) && HidePositions[x] == null).OrderBy(x => x.GetDistance(agent.transform.position));
            foreach (var key in positions)
            {
                HidePositions[key] = agent;
                return key;
            }
        }

        return Vector3.zero;
    }
}
```

에이전트는 에이전트 뒤에 있는 오브젝트에 숨으려고 시도하지만, 실패하면 목표를 지나치더라도 다른 곳에 숨으려고 시도합니다.

숨을 수 있는 위치가 더 이상 없다면, hidingState는 moduleData에서 OnFailedToFindWay() 함수를 호출합니다.

</details>

</details>

</details>

<details>
<summary>Checking</summary>

1. checking 스테이트는 다른 스테이트와는 다르게 동작합니다.

단순히 체킹을 하는 용도이기 때문에 스테이트를 바꾸지 않습니다. 

```csharp

public void StartState(NavAgentModule.NavAgentModuleData moduleData, Action onDoneHandler)
{
    switch (moduleData.State)
    {
        case NavAgentModule.StateList.Pointing:
        case NavAgentModule.StateList.Tracking:
        case NavAgentModule.StateList.Hiding:
            OnDoneHandler = onDoneHandler;
            isInterrupting = StateModuleHandler.IsPlayingModuleRunning();
            StateModuleHandler.EnterModule((int)moduleData.State, moduleData);
            break;
        // checking module is works different cause it just for cheking that agent could reach to target or not.
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
```

2. 또한 StateModuleHandler을 사용하지 않음으로, 스테이트가 끝날때 호출하던 onDoneHandler는 사용되지 않습니다.

이것의 대안으로 CheckingOption의 OnFindWay를 사용할 수 있습니다.

```csharp
public class NavAgentModuleData : StateModuleData
{
    public BasicOption BasicModulerSetting { private set; get; }
    public CheckingOption CheckingMoudlerSetting { private set; get; }

    public NavAgentModuleData(BasicOption basicOption, CheckingOption checkingOption)
    {
        BasicModulerSetting = basicOption;
        CheckingMoudlerSetting = checkingOption;
    }

    public class CheckingOption
    {
        public Action<bool> OnFindWay { private set; get; } = null;
        public CheckingOption(Action<bool> onFindWay)
        {
            OnFindWay = onFindWay;
        }
    }
}
```

3. checking 스테이트는, 에이저트가 목표에 도달 할 수 있는지 여부만 알려줍니다.

다시 말하지만. 이것은 단순 확인용임으로 스테이트를 바꾸지 않습니다.