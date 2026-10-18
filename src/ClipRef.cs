namespace Obentunes
{
    public abstract class ClipRef
    {
        protected ClipRef(string name, string displayName)
        {
            Name = name;
            DisplayName = displayName;
        }

        public string Name { get; }
        public string DisplayName { get; }
    }
    
    public class GameClipRef : ClipRef
    {
        public GameClipRef(string name, string displayName, string resourceFile, long offset, int size) 
            : base(name, displayName)
        {
            ResourceFile = resourceFile;
            Offset = offset;
            Size = size;
        }
        
        public string ResourceFile { get; }
        public long Offset { get; }
        public int Size { get; }
    }

    public class CustomClipRef : ClipRef
    {
        public CustomClipRef(string name, string displayName, string path)
            : base(name, displayName)
        {
            Path = path;
        }
        
        public string Path { get; }
    }
}