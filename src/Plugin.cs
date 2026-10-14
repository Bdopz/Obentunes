using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Obentunes
{
    [BepInPlugin("com.bdopz.obentunes", "Obentunes", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
    
        private static Harmony _harmony;
    
        private void Awake()
        {
            Log = Logger;
            Probe.Init(Application.dataPath, Path.Combine(Paths.BepInExRootPath, "plugins", "Obentunes"));

            _harmony = new Harmony("com.bdopz.obentunes");
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

    public class RadioMenu : MonoBehaviour
    {
        private static RadioMenu _instance;
        private Radio _radio;
        private bool _open;
    
        private Vector2 _scroll;
        private List<GameClipRef> _tracks;
        private string _loading;
        private string _track;
        private readonly Dictionary<string, string> _custom = new();

        public static bool IsOpen => _instance != null && _instance._open;

        private void Awake()
        {
            _instance = this;
        }

        public static void Open(Radio radio)
        {
            ClipFetcher.IndexClips();
        
            _instance._radio = radio;
            _instance._open = true;
            _instance._loading = null;

            GameController.instance.ControlsDisabled(_instance.gameObject, true, false);
        }
    
        public static void OnEscape()
        {
            if (IsOpen) _instance.Close();
        }

        public static bool TryResumeTrack(Radio radio)
        {
            if (!_instance._custom.TryGetValue(GetKey(radio), out string clipName)) return false;
        
            _instance.StartCoroutine(_instance.PlayTrack(radio, clipName, true));
            return true;
        }
    
        private static string GetKey(Radio r)
        {
            string character = SaveController.CharacterName ?? "";
            character = Path.GetFileName(character.TrimEnd('/', '\\'));
        
            var p = r.transform.position;
            var pos = $"{Mathf.RoundToInt(p.x * 10)},{Mathf.RoundToInt(p.y * 10)},{Mathf.RoundToInt(p.z * 10)}";
        
            return $"{r.gameObject.scene.name}|{pos}|{character}";
        }

        private void Close()
        {
            _radio = null;
            _open = false;
            GameController.instance.ControlsEnabled(gameObject);
        }

        private void BuildSongs()
        {
            var all = ClipFetcher.Clips;
            if (all == null) return; // still indexing

            _tracks = all
                .OrderBy(c => c.DisplayName)
                .ToList();
        }

        private IEnumerator PlayTrack(Radio radio, string clipName, bool force = false)
        {
            _custom.TryGetValue(GetKey(radio), out string currentClipName);
            if (!force && currentClipName == clipName && radio.CurrentState > 0) yield break;

            _loading = clipName;
            AudioClip clip = null;
            yield return ClipFetcher.LoadClip(clipName, c => clip = c);
            _loading = null;

            if (clip == null || radio == null || !radio.RadioEnabled) yield break;

            radio.CurrentState = 2;
            foreach (var o in radio.OnTurnOn) o.Fire();
        
            radio.GetComponent<MeshRenderer>().material = radio.onMaterial;
        
            radio.soundInfo.ChangeClip(clip);
            radio.soundInfo.PlaySound();
            _track = ClipFetcher.GetDisplayName(clipName);
        
            if (currentClipName != null && currentClipName != clipName) ClipFetcher.UnloadClip(currentClipName);
            _custom[GetKey(radio)] = clipName;
        }

        private void OnGUI()
        {
            if (!_open) return;
            if (_tracks == null) BuildSongs();
        
            GUI.skin = UiStyles.GetOrBuildSkin();

            var rect = new Rect(Screen.width / 2f - 225, Screen.height / 2f - 250, 450, 500);
            GUI.Box(rect, GUIContent.none);
        
            var area = new Rect(rect.x + 10, rect.y + 10, rect.width - 20, rect.height - 20);
            GUILayout.BeginArea(area);

            string playing = _loading != null ? "Loading..."
                : _track != null ? $"Playing: {_track}"
                : "Nothing";
        
            GUILayout.BeginHorizontal(GUILayout.Height(40));
            GUILayout.Label(playing, "track", 
                GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUILayout.FlexibleSpace();
        
            GUILayout.BeginVertical(GUILayout.ExpandWidth(false));
            GUILayout.Label("Obentunes", "subtitle", GUILayout.ExpandWidth(true));
            GUILayout.Label("Radio", "title", GUILayout.ExpandWidth(false));
            GUILayout.EndVertical();
        
            GUILayout.EndHorizontal();
        
            GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
        
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            _scroll = GUILayout.BeginScrollView(_scroll);

            if (_tracks == null)
                GUILayout.Label("Getting Tracks...");
            else
                foreach (var track in _tracks)
                {
                    string label = track.DisplayName;
                    if (GUILayout.Button(label) && _loading == null)
                        StartCoroutine(PlayTrack(_radio, track.Name));
                }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        
            GUILayout.BeginVertical(GUILayout.Width(area.width * 0.25f));
            if (GUILayout.Button("Off", GUILayout.Height(20)))
            {
                _instance._track = null;
                _custom.Remove(GetKey(_radio));
                _radio.SetState(0);
                Close();
            }
            if (GUILayout.Button("Close (ESC)", GUILayout.Height(40))) Close();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    
        private void OnDestroy()
        {
            UiStyles.Destroy();
        }
    }
}