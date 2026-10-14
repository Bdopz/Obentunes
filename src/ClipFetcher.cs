using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BepInEx;
using Fmod5Sharp;
using UnityEngine;
using UnityEngine.Networking;

namespace Obentunes
{
    public static class ClipFetcher
    {
        private static bool _indexed;
        private static List<GameClipRef> _clips;
        private static readonly Dictionary<string, AudioClip> Loaded = new();
    
        public static IReadOnlyList<GameClipRef> Clips => _clips;
    
        public static void IndexClips()
        {
            if (_indexed) return;
            _indexed = true;
        
            Plugin.Log.LogInfo("Indexing Clips...");
        
            new Thread(() =>
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
            
                try
                {
                    _clips = Probe.Build();
                    sw.Stop();
                    Plugin.Log.LogInfo($"Indexed {_clips.Count} radio clips in {sw.ElapsedMilliseconds} ms");
                }
                catch (Exception e)
                {
                    Plugin.Log.LogError($"Indexer failed: {e}");
                }
            }) { IsBackground = true }.Start();
        }
    
        public static IEnumerator LoadClip(string name, Action<AudioClip> done)
        {
            if (Loaded.TryGetValue(name, out var existing) && existing != null)
            {
                done(existing);
                yield break;
            }
        
            var gameClip = _clips?.FirstOrDefault(c => c.Name == name);
            if (gameClip == null) { Plugin.Log.LogWarning($"{name} not in index"); yield break; }
        
            string dataPath = Application.dataPath;
            string cacheDir = Path.Combine(Paths.CachePath, "Obentunes", "temp");
        
            var task = Task.Run(() => ExtractToCache(gameClip, dataPath, cacheDir));
            while (!task.IsCompleted) yield return null;

            if (task.IsFaulted)
            {
                Plugin.Log.LogError($"Extract failed: {task.Exception}"); 
                yield break;
            }
        
            string path = task.Result;
            if (path == null) yield break;
        
            AudioClip clip;
            try
            {
                var type = Path.GetExtension(path).ToLowerInvariant() == ".wav" ? AudioType.WAV : AudioType.OGGVORBIS;

                using var req = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, type);
                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Plugin.Log.LogError($"Load failed: {req.error}");
                    yield break;
                }

                clip = DownloadHandlerAudioClip.GetContent(req);
            }
            finally
            {
                try { File.Delete(path); } catch { ; }
            }
        
            clip.name = name;
            Loaded[name] = clip;
            done(clip);
        }
    
        public static void UnloadClip(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (!Loaded.TryGetValue(name, out var clip)) return;
            if (clip != null) UnityEngine.Object.Destroy(clip);
            Loaded.Remove(name);
        }

        public static string GetDisplayName(string name)
        {
            if (Clips == null) return name;
        
            foreach (var clip in Clips)
                if (clip.Name == name)
                    return clip.DisplayName;

            return name;
        }

        private static string ExtractToCache(GameClipRef clipRef, string dataPath, string cacheDir)
        {
            string src = Path.Combine(dataPath, clipRef.ResourceFile);
            var data = new byte[clipRef.Size];
            using (var fs = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fs.Seek(clipRef.Offset, SeekOrigin.Begin);
                var read = 0;
                while (read < data.Length)
                {
                    int n = fs.Read(data, read, data.Length - read);
                    if (n <= 0) throw new EndOfStreamException($"{clipRef.Name}: ran out of data");
                    read += n;
                }
            }

            var bank = FsbLoader.LoadFsbFromByteArray(data);
            if (!bank.Samples[0].RebuildAsStandardFileFormat(out byte[] bytes, out string ext) || bytes == null)
            {
                Plugin.Log.LogWarning($"{clipRef.Name}: Fmod5Sharp couldn't rebuild the sample");
                return null;
            }

            Directory.CreateDirectory(cacheDir);
            string outPath = Path.Combine(cacheDir, $"{clipRef.Name}_{Guid.NewGuid():N}.{ext}");
            File.WriteAllBytes(outPath, bytes);
        
            return outPath;
        }
    }
}