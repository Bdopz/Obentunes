using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Obentunes
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
    
        private static Harmony _harmony;
    
        private void Awake()
        {
            Log = Logger;
            Probe.Init(Application.dataPath, Path.Combine(Paths.BepInExRootPath, "plugins", "Obentunes"));

            _harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            _harmony.UnpatchSelf();
        
            gameObject.AddComponent<RadioMenu>();
            _harmony.PatchAll(typeof(Patches));
        
            Logger.LogInfo("Loaded just fine!");
        }
    }

    internal static class Patches
    {
        // Radio Patches
        
        [HarmonyPatch(typeof(Radio), nameof(Radio.Interact))]
        [HarmonyPrefix]
        private static bool HookRadioMenu(Radio __instance)
        {
            RadioMenu.Open(__instance);
            if (__instance.CurrentState == 0) __instance.SetState(1);
            return false;
        }
        
        [HarmonyPatch(typeof(Radio), nameof(Radio.UpdateUseMessage))]
        [HarmonyPostfix]
        private static void OverrideUseLabel(Radio __instance)
        {
            UseText.instance.ChangeUseText(__instance.title, "Use");
        }
        
        [HarmonyPatch(typeof(Radio), nameof(Radio.OnLoadingGame))]
        [HarmonyPostfix]
        private static void ResumeTracks(Radio __instance, bool __result)
        {
            if (!__result || __instance.CurrentState <= 0)
            {
                return;
            }
            
            if (RadioMenu.TryResumeTrack(__instance)) return;
            __instance.CurrentState = 0;
        }
        
        // Menu Patches
        
        [HarmonyPatch(typeof(GameUIController), nameof(GameUIController.IsMenuVisible), MethodType.Getter)]
        [HarmonyPostfix]
        private static void IsRadioMenuVisible(ref bool __result) => __result |= RadioMenu.IsOpen;
        
        [HarmonyPatch(typeof(GameUIController), nameof(GameUIController.HideGameMenu))]
        [HarmonyPrefix]
        private static bool HideRadiomenu(GameUIController __instance)
        {
            if (!RadioMenu.IsOpen) return true;

            RadioMenu.OnEscape();
            return __instance.GameMenu.activeInHierarchy;
        }
    }
}