using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using LumenCat92.Extentioner;
using LumenCat92.GizmosDrawer;
using System.Threading;
namespace LumenCat92.Nav
{
    public class NavHideManager : MonoBehaviour
    {
        public static NavHideManager Instance { set; get; }
        [field: SerializeField] private float SphereCastRadius { set; get; } = 5f;
        [field: SerializeField] private float MaxRefreshDistance { set; get; } = 1f;
        Dictionary<Transform, NavHideData> HideDataDic { set; get; } = new Dictionary<Transform, NavHideData>();
        [field: SerializeField] public bool ShouldDrawGizmos { set; get; }
        public enum debugingLine { ConnectAgentToCollider, ConnectColliderToHideSameplePosition, ConnectHideSameplePositionToTarget, ConnectSideToSide, ConnectFloorToEachSide, DrawLine }
        Dictionary<debugingLine, Color> debugingLineColor = new Dictionary<debugingLine, Color>();
        public enum debugingSpere { CastSize, HideSamplePosition, IsHit, IsntHit }
        Dictionary<debugingSpere, Color> debugingSpereColor = new Dictionary<debugingSpere, Color>();
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                { // debuging line color setting
                    debugingLineColor.Add(debugingLine.ConnectAgentToCollider, Color.yellow);
                    debugingLineColor.Add(debugingLine.ConnectColliderToHideSameplePosition, Color.yellow);
                    debugingLineColor.Add(debugingLine.ConnectHideSameplePositionToTarget, Color.cyan);
                    debugingLineColor.Add(debugingLine.ConnectSideToSide, Color.black);
                    debugingLineColor.Add(debugingLine.ConnectFloorToEachSide, Color.black);
                    debugingLineColor.Add(debugingLine.DrawLine, Color.black);
                }

                {
                    debugingSpereColor.Add(debugingSpere.CastSize, Color.yellow - new Color(0, 0, 0, 0.7f));
                    debugingSpereColor.Add(debugingSpere.HideSamplePosition, Color.yellow);
                    debugingSpereColor.Add(debugingSpere.IsHit, Color.green);
                    debugingSpereColor.Add(debugingSpere.IsntHit, Color.red);
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
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

        // collecting hide position near by agent
        List<Vector3> GetHidePosition(Vector3 targetPosition, NavMeshAgent agent, int layerMask = ~0)
        {
            var agentHeight = agent.transform.lossyScale.y * agent.height - agent.baseOffset;
            var agentRadius = agent.radius * Math.Max(agent.transform.lossyScale.x, agent.transform.lossyScale.z);
            var colliders = agent.transform.position.GetOverlapSphere(SphereCastRadius).Where(x => IsObjAvailable(x, layerMask)).ToList();
            GizmosDrawerManager.Instance.DrawSphere(agent.transform.position, SphereCastRadius, 2f, debugingSpereColor[debugingSpere.HideSamplePosition], ShouldDrawGizmos);
            var path = new List<Vector3>();
            var tempList = new List<Vector3>();
            var filter = new NavMeshQueryFilter
            {
                agentTypeID = agent.agentTypeID,
                areaMask = NavMesh.AllAreas
            };
            colliders.ForEach(collider =>
            {
                var list = GetHidePointFromColider(collider, targetPosition, agent.transform.position, agentRadius, agentHeight, filter, layerMask);
                if (list.Count > 0)
                {
                    if (path.Count == 0)
                    {
                        path.Add(list[0]);
                        list.RemoveAt(0);
                    }

                    for (int i = 0; i < list.Count; i++)
                    {
                        var canAdd = true;
                        var positionFromCollider = list[i];
                        for (int j = 0; j < path.Count; j++)
                        {
                            var applyedPosition = path[j];
                            if (positionFromCollider.GetDistance(applyedPosition) < agentRadius * 2.5f)
                            {
                                canAdd = false;
                                break;
                            }
                        }

                        if (canAdd)
                        {
                            path.Add(positionFromCollider);
                            GizmosDrawerManager.Instance.DrawSphere(positionFromCollider, 0.02f, 2f, debugingSpereColor[debugingSpere.IsHit], ShouldDrawGizmos);
                        }
                    }

                    path.AddRange(tempList);
                    tempList.Clear();
                }
            });

            return path;
        }

        // get hide position from collider as much as possible
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
        bool IsCoveredByOtherObj(Vector3 targetPosition, Vector3 centerPosition, int layerMask)
        {
            var isHit = centerPosition.RayCast(centerPosition.GetDirection(targetPosition), out RaycastHit hit);
            GizmosDrawerManager.Instance.DrawLine(centerPosition, hit.point, 2f, debugingLineColor[debugingLine.DrawLine], ShouldDrawGizmos);
            isHit = isHit ? IsObjAvailable(hit.collider, layerMask) : false;

            return isHit;
        }

        bool IsObjAvailable(Collider collider, int layerMask)
        {
            return !collider.isTrigger &&
                    (collider.gameObject.isStatic ||
                   (layerMask & 1 << collider.gameObject.layer) != 0);
        }

        bool IsColliderExist(Collider collider, List<RaycastHit> hits)
        {
            if (collider != null)
            {
                foreach (var item in hits)
                {
                    if (item.collider == collider)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

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
    }
}