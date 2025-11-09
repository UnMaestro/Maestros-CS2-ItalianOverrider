using Colossal.Entities;
using Game;
using Game.Buildings;
using Game.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities;

#if BURST
using Unity.Burst;
#endif
namespace MaestroItalianOverrider.Systems
{
    public partial class EdgeExtraDataUpdater : GameSystemBase
    {
        private EndFrameBarrier m_endFrameBarrier;
        private readonly Queue<Action<EntityCommandBuffer>> m_actionsToRun = new();
        private readonly HashSet<Entity> m_edgesToWork = new();
        private static EdgeExtraDataUpdater Instance { get; set; }

        public static void EnqueueToRun(Action<EntityCommandBuffer> action)
        {
            Instance.m_actionsToRun.Enqueue(action);
        }

        public static void UpdateEdgeData(Entity edge)
        {
            Instance.m_edgesToWork.Add(edge);
        }

        public struct EdgeNodeInformation : IComponentData
        {
            public int minNumber;
            public int maxNumber;
            public Colossal.Hash128 VersionIdentifier;
        }


        protected override void OnCreate()
        {
            base.OnCreate();
            Instance = this;
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            m_endFrameBarrier = World.GetExistingSystemManaged<EndFrameBarrier>();
        }

        protected override void OnUpdate()
        {
            EntityCommandBuffer ecb = default;
            if (m_actionsToRun.Count > 0)
            {
                ecb = m_endFrameBarrier.CreateCommandBuffer();
                while (m_actionsToRun.TryDequeue(out var action))
                {
                    action(ecb);
                }
            }
            if (m_edgesToWork.Count > 0)
            {
                var itemsToWork = m_edgesToWork.ToArray();
                m_edgesToWork.Clear();
                foreach (var edge in itemsToWork)
                {
                    if (EntityManager.HasComponent<EdgeExtraDataUpdater.EdgeNodeInformation>(edge))
                    {
                        continue;
                    }
                    if (!ecb.IsCreated) ecb = m_endFrameBarrier.CreateCommandBuffer();

                    var edgeNodesData = EntityManager.GetComponentData<Edge>(edge);
                    var edgeCurve = EntityManager.GetComponentData<Curve>(edge);

                    BuildingUtils.GetAddress(EntityManager, default, edge, 0.05f, out _, out var startNumber);
                    BuildingUtils.GetAddress(EntityManager, default, edge, 0.95f, out _, out var endNumber);

                    if (endNumber < startNumber)
                    {
                        (startNumber, endNumber) = (endNumber, startNumber);
                    }
                    var edgeNodeInfo = new EdgeExtraDataUpdater.EdgeNodeInformation
                    {
                        minNumber = startNumber,
                        maxNumber = endNumber,
                        VersionIdentifier = Guid.NewGuid()
                    };
                    ecb.AddComponent(edge, edgeNodeInfo);
                    if (EntityManager.TryGetComponent<Aggregated>(edge, out var agg))
                    {
                        if (EntityManager.TryGetBuffer<ConnectedEdge>(edgeNodesData.m_Start, true, out var startEdges) &&
                         startEdges.Length == 2)
                        {
                            var otherEdge = startEdges[0].m_Edge == edge ? startEdges[1].m_Edge : startEdges[0].m_Edge;
                            if (!EntityManager.HasComponent<EdgeExtraDataUpdater.EdgeNodeInformation>(edge)
                                && EntityManager.TryGetComponent<Aggregated>(otherEdge, out var otherAgg)
                                && otherAgg.m_Aggregate == agg.m_Aggregate)
                            {
                                m_edgesToWork.Add(otherEdge);
                            }
                        }
                        if (EntityManager.TryGetBuffer<ConnectedEdge>(edgeNodesData.m_End, true, out var endEdges) &&
                         endEdges.Length == 2)
                        {
                            var otherEdge = endEdges[0].m_Edge == edge ? endEdges[1].m_Edge : endEdges[0].m_Edge;
                            if (!EntityManager.HasComponent<EdgeExtraDataUpdater.EdgeNodeInformation>(edge)
                                && EntityManager.TryGetComponent<Aggregated>(otherEdge, out var otherAgg)
                                && otherAgg.m_Aggregate == agg.m_Aggregate)
                            {
                                m_edgesToWork.Add(otherEdge);
                            }
                        }
                    }
                }
            }
        }
    }
}