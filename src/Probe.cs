using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AssetsTools.NET.Extra;
using Newtonsoft.Json;

namespace Obentunes
{
    public class GameClipRef
    {
        public GameClipRef(string name, string displayName, string resourceFile, long offset, int size)
        {
            Name = name;
            DisplayName = displayName;
            ResourceFile = resourceFile;
            Offset = offset;
            Size = size;
        }

        public string Name { get; }
        public string DisplayName { get; }
        public string ResourceFile { get; }
        public long Offset { get; }
        public int Size { get; }
    }

    public static class Probe
    {
        private static readonly IReadOnlyList<string> ClipKeywords = new[] { "radio_", "music_" }; 
        
        private static string _pluginPath, _dataPath, _resPath;
        private static Dictionary<string, string> _trackNames;

        public static void Init(string dataPath, string pluginPath)
        {
            _dataPath = dataPath;
            _pluginPath = pluginPath;
            _resPath = Path.Combine(_pluginPath, "res");
            
            _trackNames = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                File.ReadAllText(Path.Combine(_resPath, "TrackNames.json")));
        }

        public static List<GameClipRef> Build()
        {
            var result = new List<GameClipRef>();
            var unmapped = new List<string>();
            
            var manager = new AssetsManager();
            manager.LoadClassPackage(Path.Combine(_resPath, "classdata.tpk"));

            foreach (string file in Directory.GetFiles(_dataPath, "sharedassets*.assets"))
            {
                var instance = manager.LoadAssetsFile(file);
                manager.LoadClassDatabaseFromPackage(instance.file.Metadata.UnityVersion);

                foreach (var info in instance.file.GetAssetsOfType(AssetClassID.AudioClip))
                {
                    var baseField = manager.GetBaseField(instance, info);
                    string name = baseField["m_Name"].AsString;
                    
                    if (!ClipKeywords.Any(name.Contains)) continue;
                    
                    if (!_trackNames.TryGetValue(name, out string displayName))
                    {
                        unmapped.Add(name);
                        continue;
                    }

                    var resource = baseField["m_Resource"];
                    result.Add(new GameClipRef(
                        name,
                        displayName,
                        Path.GetFileName(resource["m_Source"].AsString),
                        resource["m_Offset"].AsLong,
                        (int) resource["m_Size"].AsLong));
                }
                
                manager.UnloadAssetsFile(file);
            }
            
            manager.UnloadAll();

            if (unmapped.Count > 0)
            {
                unmapped.Sort(StringComparer.Ordinal);
                Plugin.Log.LogInfo($"Unmapped radio_ clips ({unmapped.Count}): {string.Join(", ", unmapped)}");
            }

            return result;
        }
    }
}