using System;
using System.Collections.Generic;
using System.IO;
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
        private static string _pluginPath;
        private static string _dataPath;
        private static Dictionary<string, string> _trackNames;

        public static void Init(string dataPath, string pluginPath)
        {
            _dataPath = dataPath;
            _pluginPath = pluginPath;
            
            _trackNames = JsonConvert.DeserializeObject<Dictionary<string, string>>(
                File.ReadAllText(Path.Combine(_pluginPath, "res", "TrackNames.json")));
        }

        public static List<GameClipRef> Build()
        {
            var result = new List<GameClipRef>();
            var unmapped = new List<string>();
            var am = new AssetsManager();
            am.LoadClassPackage(Path.Combine(_pluginPath, "res", "classdata.tpk"));

            foreach (string file in Directory.GetFiles(_dataPath, "sharedassets*.assets"))
            {
                var inst = am.LoadAssetsFile(file);
                am.LoadClassDatabaseFromPackage(inst.file.Metadata.UnityVersion);

                foreach (var info in inst.file.GetAssetsOfType(AssetClassID.AudioClip))
                {
                    var bf = am.GetBaseField(inst, info);
                    string name = bf["m_Name"].AsString;

                    if (!_trackNames.TryGetValue(name, out string displayName))
                    {
                        if (name.StartsWith("radio_")) unmapped.Add(name);
                        continue;
                    }

                    var res = bf["m_Resource"];
                    result.Add(new GameClipRef(
                        name,
                        displayName,
                        Path.GetFileName(res["m_Source"].AsString),
                        res["m_Offset"].AsLong,
                        (int)res["m_Size"].AsLong));
                }
                am.UnloadAssetsFile(file);
            }

            if (unmapped.Count <= 0) return result;
            unmapped.Sort(StringComparer.Ordinal);
            Plugin.Log.LogInfo($"Unmapped radio_ clips ({unmapped.Count}): {string.Join(", ", unmapped)}");

            return result;
        }
    }
}