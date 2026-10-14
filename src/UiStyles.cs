using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Obentunes
{
    public static class UiStyles
    {
        private static readonly List<Texture2D> Textures = new();
        private static GUISkin _skin;
    
        public static GUISkin GetOrBuildSkin()
        {
            if (_skin != null) return _skin;
        
            var skin = Object.Instantiate(GUI.skin);
            skin.window.normal.background = MakeTex(new Color(0.1f, 0.1f, 0.12f, 0.95f));
            skin.window.onNormal.background = skin.window.normal.background;
            skin.window.normal.textColor = Color.white;
            skin.button.normal.background = MakeTex(new Color(0.2f, 0.2f, 0.25f));
            skin.button.hover.background = MakeTex(new Color(0.3f, 0.3f, 0.4f));
            skin.button.onHover.background = skin.button.hover.background;
            skin.label.fontSize = 16;
            skin.label.normal.textColor = Color.white;
        
            skin.verticalScrollbarThumb.normal.background = MakeTex(new Color(0.2f, 0.3f, 0.4f));
        
            var title = new GUIStyle(skin.label)
            {
                name = "title", 
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleRight,
                fontSize = 26, 
                wordWrap = false,
                margin =
                {
                    top = 0
                },
                padding =
                {
                    top = 0
                }
            };

            var subtitle = new GUIStyle(skin.label)
            {
                name = "subtitle", 
                alignment = TextAnchor.MiddleRight,
                fontSize = 13,
                margin =
                {
                    bottom = 0
                },
                padding =
                {
                    bottom = 0
                }
            };

            var track = new GUIStyle(skin.label)
            {
                name = "track",
                wordWrap = false,
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip 
            };
        
            skin.customStyles = new[] { title, subtitle, track };

            _skin = skin;
            return skin;
        }
    
        private static Texture2D MakeTex(Color c)
        {
            var t = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            t.SetPixel(0, 0, c);
            t.Apply();
            Textures.Add(t);
            return t;
        }
    
        public static void Destroy()
        {
            foreach (var t in Textures.Where(t => t != null)) Object.Destroy(t);
            Textures.Clear();
            if (_skin != null) Object.Destroy(_skin);
            _skin = null;
        }
    }
}