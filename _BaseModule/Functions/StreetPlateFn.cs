using Belzont.Utils;
using BridgeWE;
using Colossal.Entities;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Net;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using MaestroItalianOverrider.Systems;

namespace MaestroItalianOverrider.Functions
{
    public static class StreetPlateFn
    {
        public struct StreetPlateCacheData : IComponentData
        {
            public int thisMinNumber;
            public int thisMaxNumber;
            public int otherMinNumber;
            public int otherMaxNumber;
            public bool thisNodeIsMinNumber;
            public bool otherNodeIsMinNumber;
            public float thisRoadAngle;
            public float otherRoadAngle;
            public Entity thisEdge;
            public Entity otherEdge;
            public Colossal.Hash128 thisVersion;
            public Colossal.Hash128 otherVersion;
            public float3 offsetPosition;
            internal bool _isOddOnMainProp;
            internal float _targetAngle;
            internal float _otherRoadAngle;
            internal float _thisRoadAngle;
            internal float3 _originalPos;
            internal Bezier4x3 _nodeCenterCurve;

            public readonly bool IsUpToDate(Entity edge, Colossal.Hash128 version)
                => (edge == thisEdge && version == thisVersion) || (edge == otherEdge && version == otherVersion);
        }

        public static StreetNumberRange GetNumberRange(Entity reference, Dictionary<string, string> vars)
            => RunWithCache(reference, vars, default, GenerateNumberText);


        public static float3 GetSignDirectionAngle(Entity reference, Dictionary<string, string> vars)
            => RunWithCache(
                reference,
                vars,
                default,
                (_, data, cachedData) => new float3(0, data.RefEdge == cachedData.thisEdge ? cachedData.thisRoadAngle : cachedData.otherRoadAngle, 0)
                );
        public static float3 GetSignOffsetPosition(Entity reference, Dictionary<string, string> vars)
            => RunWithCache(reference, vars, default, (_, data, cachedData) => cachedData.offsetPosition);



        private delegate T StreetPlateCacheResultGetter<T>(
          Dictionary<string, string> vars,
          (Entity RefEdge, ushort AzimuthDirection16bits, float3 CenterPoint, float3 RefPoint, Colossal.Hash128 VersionIdentifier) data,
          StreetPlateCacheData cachedData
            );

        private static T RunWithCache<T>(Entity reference, Dictionary<string, string> vars, T defaultValue, StreetPlateCacheResultGetter<T> resultGetterFn)
        {
            var data = WERoadFnBridge.GetFromPropByTargetVar(reference, vars);
            if (data.RefEdge == Entity.Null)
            {
                return defaultValue;
            }
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            if (em.TryGetComponent<StreetPlateCacheData>(reference, out var cachedData))
            {
                if (cachedData.IsUpToDate(data.RefEdge, data.VersionIdentifier))
                {
                    return resultGetterFn(vars, data, cachedData);
                }
                else
                {
                    EdgeExtraDataUpdater.EnqueueToRun((ecb) =>
                    {
                        ecb.RemoveComponent<StreetPlateCacheData>(reference);
                    });
                }
            }

            cachedData = UpdateCache(reference, em);
            if (cachedData.thisEdge == Entity.Null)
            {
                return defaultValue;
            }

            return resultGetterFn(vars, data, cachedData);
        }

        private static StreetNumberRange GenerateNumberText(Dictionary<string, string> vars, (Entity RefEdge, ushort AzimuthDirection16bits, float3 CenterPoint, float3 RefPoint, Colossal.Hash128 VersionIdentifier) data, StreetPlateCacheData cachedData)
        {
            bool isThisNode = data.RefEdge == cachedData.thisEdge;
            return new StreetNumberRange
            {
                thisNodeIsMinNumber = isThisNode ? cachedData.thisNodeIsMinNumber : cachedData.otherNodeIsMinNumber,
                minNumber = isThisNode ? cachedData.thisMinNumber : cachedData.otherMinNumber,
                maxNumber = isThisNode ? cachedData.thisMaxNumber : cachedData.otherMaxNumber
            };
        }



        private static StreetPlateCacheData UpdateCache(Entity reference, EntityManager em)
        {
            var dataThis = WERoadFnBridge.GetRoadOwnSegmentForProp(reference);
            var dataOther = WERoadFnBridge.GetRoadSideSegmentForProp(reference);

            MapNumberRange(reference, dataThis, em, out bool thisNodeIsAtEnd, out int thisMinNumber, out int thisMaxNumber, out bool successThis);
            MapNumberRange(reference, dataOther, em, out bool otherNodeIsAtEnd, out int otherMinNumber, out int otherMaxNumber, out bool successOther);
            if (!successOther || !successThis)
            {
                return default;
            }

            var thisRoadAngle = dataThis.AzimuthDirection16bits / 65536f * 360;
            var otherRoadAngle = dataOther.AzimuthDirection16bits / 65536f * 360;
            em.TryGetComponent<Game.Objects.Transform>(reference, out var transform);

            em.TryGetComponent<Owner>(reference, out var owner);
            em.TryGetComponent<Node>(owner.m_Owner, out var nodeData);
            em.TryGetBuffer<Game.Net.SubLane>(owner.m_Owner, true, out var lanes);

            var propPosition = transform.m_Position;
            var nearestDistanceAngle = float.MaxValue;

            em.TryGetComponent<Curve>(dataThis.RefEdge, out var curveThis);
            var midCurve = MathUtils.Position(curveThis.m_Bezier, .5f);
            var nodeCenterCurve = new Bezier4x3(nodeData.m_Position, dataThis.CenterPoint, midCurve, midCurve);
            var isOnOddSideMainProp = IsOddSide(transform.m_Position, nodeCenterCurve, .5f);
            float targetAngle = (thisRoadAngle + otherRoadAngle + ((thisRoadAngle > otherRoadAngle) == isOnOddSideMainProp ? 360 : 0)) / 2 % 360;


            for (int i = 0; i < lanes.Length; i++)
            {
                if ((lanes[i].m_PathMethods & Game.Pathfind.PathMethod.Pedestrian) != 0
                     && em.TryGetComponent<PedestrianLane>(lanes[i].m_SubLane, out var laneData)
                     && (laneData.m_Flags & PedestrianLaneFlags.Crosswalk) == 0
                    && em.TryGetComponent<Curve>(lanes[i].m_SubLane, out var laneCurve))
                {
                    MathUtils.Distance(laneCurve.m_Bezier.xz, dataThis.CenterPoint.xz, out float pos);
                    var angle = dataThis.CenterPoint.xz.GetAngleToPoint(MathUtils.Position(laneCurve.m_Bezier, pos).xz);
                    var diffAngle = math.abs(targetAngle - angle);
                    if (diffAngle > 180) diffAngle = 360 - diffAngle;

                    if (diffAngle < nearestDistanceAngle)
                    {
                        nearestDistanceAngle = diffAngle;
                        propPosition = MathUtils.Position(laneCurve.m_Bezier, pos);
                    }

                }
            }

            StreetPlateCacheData cacheData = new()
            {
                thisMinNumber = thisMinNumber,
                thisMaxNumber = thisMaxNumber,
                thisEdge = dataThis.RefEdge,
                otherEdge = dataOther.RefEdge,
                thisVersion = dataThis.VersionIdentifier,
                otherVersion = dataOther.VersionIdentifier,
                otherMaxNumber = otherMaxNumber,
                otherMinNumber = otherMinNumber,
                thisRoadAngle = thisRoadAngle - ((Quaternion)transform.m_Rotation).eulerAngles.y,
                otherRoadAngle = otherRoadAngle - ((Quaternion)transform.m_Rotation).eulerAngles.y,
                thisNodeIsMinNumber = thisNodeIsAtEnd,
                otherNodeIsMinNumber = otherNodeIsAtEnd,
                offsetPosition = new float3(((float3)(Matrix4x4.Rotate(transform.m_Rotation).inverse.MultiplyPoint(propPosition - transform.m_Position))).xz, 0).xzy,
                _targetAngle = targetAngle,
                _originalPos = transform.m_Position,
                _thisRoadAngle = thisRoadAngle,
                _otherRoadAngle = otherRoadAngle,
                _isOddOnMainProp = isOnOddSideMainProp,
                _nodeCenterCurve = nodeCenterCurve

            };
            EdgeExtraDataUpdater.EnqueueToRun((ecb) =>
            {
                ecb.AddComponent(reference, cacheData);
            });
            return cacheData;
        }

        private static bool IsOddSide(float3 refPos, Bezier4x3 curve, float curvePos, bool inverted = false)
        {
            float2 x2 = refPos.xz - MathUtils.Position(curve, curvePos).xz;
            float2 y2 = MathUtils.Right(MathUtils.Tangent(curve, curvePos).xz);
            return (math.dot(x2, y2) > 0f) != inverted;
        }

        private static void MapNumberRange(Entity reference,
            (Entity RefEdge, ushort AzimuthDirection16bits, Unity.Mathematics.float3 CenterPoint, Unity.Mathematics.float3 RefPoint, Colossal.Hash128 VersionIdentifier) data,
            EntityManager em, out bool nodeIsMinNumber, out int minNumber, out int maxNumber, out bool success)
        {
            var nextEdge = data.RefEdge;
            var refNode = em.GetComponentData<Owner>(reference).m_Owner;
            minNumber = int.MaxValue;
            maxNumber = int.MinValue;
            success = true;
            while (em.TryGetComponent<Edge>(nextEdge, out var edgeNodes))
            {
                if (!em.TryGetComponent<EdgeExtraDataUpdater.EdgeNodeInformation>(nextEdge, out var edgeNodeInfo))
                {
                    EdgeExtraDataUpdater.UpdateEdgeData(nextEdge);
                    success = false;
                }
                else
                {
                    if (edgeNodeInfo.minNumber < minNumber)
                    {
                        minNumber = edgeNodeInfo.minNumber;
                    }
                    if (edgeNodeInfo.maxNumber > maxNumber)
                    {
                        maxNumber = edgeNodeInfo.maxNumber;
                    }
                }
                var otherNode = edgeNodes.m_Start == refNode
                    ? edgeNodes.m_End
                    : edgeNodes.m_Start;

                if (em.TryGetBuffer<ConnectedEdge>(otherNode, true, out var edgesConnected)
                    && edgesConnected.Length == 2
                    && em.TryGetComponent<Aggregated>(edgesConnected[0].m_Edge, out var agg0)
                    && em.TryGetComponent<Aggregated>(edgesConnected[1].m_Edge, out var agg1)
                    && agg0.m_Aggregate == agg1.m_Aggregate)
                {
                    nextEdge = edgesConnected[0].m_Edge == nextEdge
                        ? edgesConnected[1].m_Edge
                        : edgesConnected[0].m_Edge;
                    refNode = otherNode;
                }
                else
                {
                    break;
                }
            }

            BuildingUtils.GetAddress(em, reference, data.RefEdge, 0f, out _, out var startSegmentNumber);
            var numberSide = startSegmentNumber & 1;
            maxNumber &= ~1;
            minNumber &= ~1;
            maxNumber |= numberSide;
            minNumber |= numberSide;


            BuildingUtils.GetAddress(em, reference, data.RefEdge, 1f, out _, out var endSegmentNumber);

            var edgeCurve = em.GetComponentData<Curve>(data.RefEdge);

            nodeIsMinNumber = (Vector3)edgeCurve.m_Bezier.a == (Vector3)data.CenterPoint == (startSegmentNumber < endSegmentNumber);
        }


        public struct StreetNumberRange
        {
            public bool thisNodeIsMinNumber;
            public int maxNumber;
            public int minNumber;

            public readonly string FormatWithSeparator(Dictionary<string, string> vars)
            {
                var separator = vars.TryGetValue("separator", out var sep) ? sep : " ~ ";
                return vars.ContainsKey("inverted") != thisNodeIsMinNumber ? $"{maxNumber}{separator}{minNumber}" : $"{minNumber}{separator}{maxNumber}";
            }
        }
    }
}
