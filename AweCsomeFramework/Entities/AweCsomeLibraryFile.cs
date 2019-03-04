using System.IO;

namespace AweCsome.Entities
{
    public class AweCsomeLibraryFile
    {
        public string Filename { get; set; }
        public Stream Stream { get; set; }
        public object Entity { get; set; }
    }
}
