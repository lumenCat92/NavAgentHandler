using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LumenCat92.SimpleFSM;

namespace LumenCat92.Nav
{
    public abstract partial class NavAgentModule
    {
        public class NavAgentModuleData : StateModuleData
        {
            public BasicOption BasicModulerSetting { private set; get; }
            public PointingOption PointingModulerSetting { private set; get; }
            public TrackingOption TrackingModulerSetting { private set; get; }
            public HidingOption HidingMoudlerSetting { private set; get; }
            public CheckingOption CheckingMoudlerSetting { private set; get; }
            public StateList State
            {
                get
                {
                    return PointingModulerSetting != null ? StateList.Pointing :
                            TrackingModulerSetting != null ? StateList.Tracking :
                            HidingMoudlerSetting != null ? StateList.Hiding :
                            CheckingMoudlerSetting != null ? StateList.Cheking : StateList.Non;
                }
            }
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
                public Transform Target { private set; get; }
                public bool IsLookAtOn { private set; get; }
                public float StopDist { private set; get; }
                public float AgentSpeed { private set; get; }
                public bool ShouldActiveDeadLockCheck { private set; get; }
                public Action OnFailedToFindWay { private set; get; }

                public BasicOption(Transform target, bool isLookAtOn, float stopDist, float agentSpeed, bool shouldActiveDeadLockCheck, Action doWhenCantFindWay)
                {
                    Target = target;
                    IsLookAtOn = isLookAtOn;
                    StopDist = stopDist;
                    AgentSpeed = agentSpeed;
                    ShouldActiveDeadLockCheck = shouldActiveDeadLockCheck;
                    OnFailedToFindWay = doWhenCantFindWay;
                }
            }

            public class PointingOption
            {
                public float StayingTime { private set; get; } = 0f;
                public bool IsUnlimitedStayTime => StayingTime <= -1;
                public bool CanPositionChange { private set; get; } = true;
                public TrafficDataSet TrafficDataSetting { private set; get; } = new TrafficDataSet();
                public PointingOption(float stayingTime, bool canPositionChange, TrafficDataSet trafficDataSet = null)
                {
                    StayingTime = stayingTime;
                    CanPositionChange = canPositionChange;
                    TrafficDataSetting = trafficDataSet == null ? this.TrafficDataSetting : trafficDataSet;
                }
                public class TrafficDataSet
                {
                    public int MaxWaitingCount { private set; get; } = 3;
                    public float MaxStayingTime { private set; get; } = 3f;
                    public TrafficDataSet() { }
                    public TrafficDataSet(int maxAddingCount, float maxStayingTime)
                    {
                        MaxWaitingCount = maxAddingCount;
                        MaxStayingTime = maxStayingTime;
                    }
                }
            }

            public class CheckingOption
            {
                public Action<bool> OnFindWay { private set; get; } = null;
                public CheckingOption(Action<bool> onFindWay)
                {
                    OnFindWay = onFindWay;
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

            public class HidingOption
            {
                public LayerMask HidableLayer { private set; get; }

                public HidingOption(LayerMask hidableLayer)
                {
                    HidableLayer = hidableLayer;
                }
            }
        }
    }
}