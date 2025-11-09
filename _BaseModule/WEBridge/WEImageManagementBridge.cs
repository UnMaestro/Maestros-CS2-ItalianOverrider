using System;
using System.Reflection;

namespace BridgeWE
{
    public static class WEImageManagementBridge
    {
        public static string GetImageAtlasVersion() => throw new NotImplementedException("Stub only!");
        public static void RegisterImageAtlas(Assembly mainAssembly, string atlasName, string[] imagePaths, Action<string> onCompleteLoading = null)
            => throw new NotImplementedException("Stub only!");
        public static bool CheckImageAtlasExists(Assembly mainAssembly, string atlasName) => throw new NotImplementedException("Stub only!");
        public static void EnsureAtlasDeleted(Assembly mainAssembly, string atlasName) => throw new NotImplementedException("Stub only!");
    }
}