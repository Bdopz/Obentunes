using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Obentunes
{
    public class RadioState
    {
        private const float DefaultVolume = 0.5f;
        
        public string CurretClip;
        public string CurrentClipName;
        public string Loading;
        public float Volume = DefaultVolume;
    }
    
    public class RadioMenu : MonoBehaviour
    {
        private readonly Dictionary<string, RadioState> _states = new();
        
        private static RadioMenu _instance;
        
        private Radio _radio;
        private RadioState _state;
        private bool _open;
        private List<ClipRef> _tracks;
        
        private Vector2 _scroll;
        private string _search = "";
        
        private List<ClipRef> Filtered => _tracks.Where(clip => 
            clip.DisplayName.Contains(_search, StringComparison.OrdinalIgnoreCase)).ToList();
        public static bool IsOpen => _instance != null && _instance._open;
        
        private void Awake()
        {
            _instance = this;
        }
        
        public static void Open(Radio radio)
        {
            ClipFetcher.IndexClips();
        
            _instance._radio = radio;
            _instance._state = GetState(radio);
            _instance._open = true;
            
            GameController.instance.ControlsDisabled(_instance.gameObject, true, false);
        }
        
        public static void OnEscape()
        {
            if (IsOpen) _instance.Close();
        }
        
        public static bool TryResumeTrack(Radio radio)
        {
            var state = GetState(radio);
            if (state.CurretClip == null) return false;

            _instance.StartCoroutine(_instance.PlayTrack(radio, state, state.CurretClip, true));
            return true;
        }
        
        private void Close()
        {
            _radio = null;
            _state = null;
            _open = false;
            
            GameController.instance.ControlsEnabled(gameObject);
        }
        
        private IEnumerator PlayTrack(Radio radio, RadioState state, string clipName, bool force = false)
        {
            if (!force && state.CurretClip == clipName && radio.CurrentState > 0) yield break;

            string previous = state.CurretClip;

            state.Loading = clipName;
            AudioClip clip = null;
            yield return ClipFetcher.LoadClip(clipName, c => clip = c);
            state.Loading = null;

            if (clip == null || radio == null || !radio.RadioEnabled) yield break;

            radio.CurrentState = 2;
            foreach (var o in radio.OnTurnOn) o.Fire();
            radio.GetComponent<MeshRenderer>().material = radio.onMaterial;

            radio.soundInfo.ChangeClip(clip);
            radio.soundInfo.SoundVolume = state.Volume;
            radio.soundInfo.PlaySound();

            state.CurretClip = clipName;
            state.CurrentClipName = ClipFetcher.GetDisplayName(clipName);

            if (previous != null && previous != clipName && !IsClipInUse(previous, state)) 
                ClipFetcher.UnloadClip(previous);
        }
        
        private bool IsClipInUse(string clipName, RadioState except) =>
            _states.Values.Any(s => s != except && s.CurretClip == clipName);
        
        private static RadioState GetState(Radio radio)
        {
            string key = GetKey(radio);
            if (!_instance._states.TryGetValue(key, out var state))
                _instance._states[key] = state = new RadioState();
            return state;
        }
        
        private static string GetKey(Radio r)
        {
            string character = SaveController.CharacterName ?? "";
            character = Path.GetFileName(character.TrimEnd('/', '\\'));
        
            var p = r.transform.position;
            var pos = $"{Mathf.RoundToInt(p.x * 10)},{Mathf.RoundToInt(p.y * 10)},{Mathf.RoundToInt(p.z * 10)}";
        
            return $"{r.gameObject.scene.name}|{pos}|{character}";
        }
        
        private void BuildSongs()
        {
            var clips = ClipFetcher.Clips;
            if (clips == null) return; // still indexing

            _tracks = clips
                .OrderBy(clip => clip.DisplayName)
                .ToList();
        }
        
        private void OnGUI()
        {
            if (!_open || _state == null) return;
            if (_tracks == null) BuildSongs();
            
            var radio = _radio;
            var state = _state;
        
            GUI.skin = UiStyles.GetOrBuildSkin();

            var rect = new Rect(Screen.width / 2f - 225, Screen.height / 2f - 250, 450, 500);
            GUI.Box(rect, GUIContent.none);
        
            var area = new Rect(rect.x + 10, rect.y + 10, rect.width - 20, rect.height - 20);
            GUILayout.BeginArea(area);

            string playing = state.Loading != null ? "Loading..."
                : state.CurretClip != null ? $"Playing: {state.CurrentClipName}"
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

            GUI.SetNextControlName("search");
            _search = GUILayout.TextField(_search, GUILayout.ExpandWidth(true));
            
            if (Event.current.type == EventType.Repaint
                && string.IsNullOrEmpty(_search)
                && GUI.GetNameOfFocusedControl() != "search")
            {
                var r = GUILayoutUtility.GetLastRect();
                GUI.Label(r, "Search...", "placeholder");
            }
            
            _scroll = GUILayout.BeginScrollView(_scroll, false, true);

            if (_tracks == null)
                GUILayout.Label("Getting Tracks...");
            else
                foreach (var track in Filtered)
                {
                    string label = track.DisplayName;
                    if (GUILayout.Button(label) && state.Loading == null)
                        StartCoroutine(PlayTrack(radio, state, track.Name));
                }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        
            GUILayout.BeginVertical(GUILayout.Width(area.width * 0.25f));
            
            if (GUILayout.Button("Off", GUILayout.Height(20)))
            {
                if (state.CurretClip != null && !IsClipInUse(state.CurretClip, state))
                    ClipFetcher.UnloadClip(state.CurretClip);
                state.CurretClip = null;
                state.CurrentClipName = null;
                radio.SetState(0);
                Close();
            }
            if (GUILayout.Button("Close (ESC)", GUILayout.Height(40))) Close();
            
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            GUILayout.BeginHorizontal(GUILayout.Height(30));
            GUILayout.Label($"Volume: {Mathf.RoundToInt(state.Volume * 100)}%", GUILayout.MaxWidth(110));

            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
            float newVolume = GUILayout.HorizontalSlider(state.Volume, 0f, 1f, GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();

            if (!Mathf.Approximately(newVolume, state.Volume))
            {
                state.Volume = newVolume;
                if (radio.soundInfo != null)
                {
                    radio.soundInfo.SoundVolume = newVolume;
                    radio.soundInfo.Volume = newVolume;
                }
            }
            GUILayout.EndHorizontal();
            
            GUILayout.EndArea();
        }
    
        private void OnDestroy()
        {
            UiStyles.Destroy();
        }
    }
}