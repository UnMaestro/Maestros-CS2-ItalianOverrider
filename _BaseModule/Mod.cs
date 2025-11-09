using BridgeWE;
using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Game;
using Game.Modding;
using Game.SceneFlow;
using HarmonyLib;
using MaestroItalianOverrider.Systems;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MaestroItalianOverrider
{
    public class Mod : IMod
    {
        public static ILog log = LogManager.GetLogger($"{typeof(Mod).Assembly.GetName().Name}.{nameof(Mod)}");
        private static readonly BindingFlags allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.GetField | BindingFlags.GetProperty;

        public void OnLoad(UpdateSystem updateSystem)
        {
            log.Info(nameof(OnLoad));

            if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
                log.Info($"Current mod asset at {asset.path}");

            GameManager.instance.RegisterUpdater(DoWhenLoaded);

            updateSystem.UpdateAt<EdgeExtraDataUpdater2B>(SystemUpdatePhase.Modification2B);
            updateSystem.UpdateAt<EdgeExtraDataUpdater>(SystemUpdatePhase.Rendering);
        }

        private void DoWhenLoaded()
        {
            log.Info($"Loading patches");
            if (DoPatches())
            {
                RegisterModFiles();
            }
        }

        private void RegisterModFiles()
        {
            GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset);
            var modDir = Path.GetDirectoryName(asset.path);

            var imagesDirectory = Path.Combine(modDir, "atlases");
            if (Directory.Exists(imagesDirectory))
            {
                var atlases = Directory.GetDirectories(imagesDirectory, "*", SearchOption.TopDirectoryOnly);
                foreach (var atlasFolder in atlases)
                {
                    WEImageManagementBridge.RegisterImageAtlas(typeof(Mod).Assembly, Path.GetFileName(atlasFolder), Directory.GetFiles(atlasFolder, "*.png"));
                }
            }

            var layoutsDirectory = Path.Combine(modDir, "layouts");
            WETemplatesManagementBridge.RegisterCustomTemplates(typeof(Mod).Assembly, layoutsDirectory);
            WETemplatesManagementBridge.RegisterLoadableTemplatesFolder(typeof(Mod).Assembly, layoutsDirectory);


            var fontsDirectory = Path.Combine(modDir, "fonts");
            WEFontManagementBridge.RegisterModFonts(typeof(Mod).Assembly, fontsDirectory);

            var objDirctory = Path.Combine(modDir, "objMeshes");

            if (Directory.Exists(objDirctory))
            {
                var meshes = Directory.GetFiles(objDirctory, "*.obj", SearchOption.AllDirectories);
                foreach (var meshFile in meshes)
                {
                    var meshName = Path.GetFileNameWithoutExtension(meshFile);
                    if (!WEMeshManagementBridge.RegisterMesh(typeof(Mod).Assembly, meshName, meshFile))
                    {
                        log.Warn($"Failed to register mesh: {meshName} from {meshFile}");
                    }
                }
            }
        }

        private bool DoPatches()
        {
            var weAsset = AssetDatabase.global.GetAsset(SearchFilter<ExecutableAsset>.ByCondition(asset => asset.isEnabled && asset.isLoaded && asset.name.Equals("BelzontWE")));
            if (weAsset?.assembly is null)
            {
                log.Error($"The module {GetType().Assembly.GetName().Name} requires Write Everywhere mod to work!");
                return false;

            }

            var exportedTypes = weAsset.assembly.ExportedTypes;
            foreach (var (type, sourceClassName) in new List<(Type, string)>() {
                    (typeof(WEFontManagementBridge), "FontManagementBridge"),
                    (typeof(WEImageManagementBridge), "ImageManagementBridge"),
                    (typeof(WETemplatesManagementBridge), "TemplatesManagementBridge"),
                    (typeof(WEMeshManagementBridge), "MeshManagementBridge"),
                    (typeof(WERoadFnBridge), "RoadFnBridge"),
                    (typeof(WELocalizationBridge), "LocalizationBridge"),
                })
            {
                var targetType = exportedTypes.First(x => x.Name == sourceClassName);
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    var srcMethod = targetType.GetMethod(method.Name, allFlags, null, method.GetParameters().Select(x => x.ParameterType).ToArray(), null);
                    if (srcMethod != null)
                    {
                        Harmony.ReversePatch(srcMethod, new HarmonyMethod(method));
                    }
                    else
                    {
                        log.Warn($"Method not found while patching WE: {targetType.FullName} {method.Name}({string.Join(", ", method.GetParameters().Select(x => $"{x.ParameterType}"))})");
                    }
                }
            }
            return true;
        }

        public void OnDispose()
        {
            log.Info(nameof(OnDispose));
        }
    }
}