using System;
using System.IO;

namespace AweCsome.Entities
{
    public class AweCsomeFile
    {
        public string Filename { get; set; }
        public Stream Stream { get; set; }
        public object Entity { get; set; }

        public enum CheckoutTypes { None, Online, Offline }
        public enum FileLevels { Checkout, Draft, Published }
        public enum VirusStatusValues {  Clean=0, Reported=1, CanBeRemoved=2, Removed=3, CantClean=4, DeleteRequested=5, Timeout=6};

        public long Length { get; set; }
        public string Version { get; set; }
        public int Author { get; set; }
        public int? CheckedOutBy { get; set; }
        public string CheckInComment { get; set; }
        public CheckoutTypes CheckoutType { get; set; }
        public FileLevels Level { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }
        public string Folder { get; set; }

        public VirusStatusValues VirusStatus { get; set; }
    }
}
