using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace BridgeWE
{
    internal static class WERoadFnBridge
    {
        public static (Entity RefEdge, ushort AzimuthDirection16bits, float3 CenterPoint, float3 RefPoint, Colossal.Hash128 VersionIdentifier) GetRoadSideSegmentForProp(Entity reference) => throw new NotImplementedException("Stub only!");
        public static (Entity RefEdge, ushort AzimuthDirection16bits, float3 CenterPoint, float3 RefPoint, Colossal.Hash128 VersionIdentifier) GetRoadOwnSegmentForProp(Entity reference) => throw new NotImplementedException("Stub only!");
        public static (Entity RefEdge, ushort AzimuthDirection16bits, float3 CenterPoint, float3 RefPoint, Colossal.Hash128 VersionIdentifier) GetFromPropByTargetVar(Entity reference, Dictionary<string, string> vars) => throw new NotImplementedException("Stub only!");
    }
}