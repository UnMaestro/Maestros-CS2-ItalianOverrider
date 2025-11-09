using System;
using System.Reflection;
using UnityEngine;

namespace BridgeWE
{
    public static class WEMeshManagementBridge
    {

        public static bool RegisterMesh(Assembly mainAssembly, string meshName, string meshObjFilePath) => throw new NotImplementedException("Stub only!");

        public static bool RegisterMeshFromMemory(Assembly mainAssembly, string meshName, Vector3[] vertices, Vector3[] normals, Vector2[] uv, int[] triangles) => throw new NotImplementedException("Stub only!");
    }
}