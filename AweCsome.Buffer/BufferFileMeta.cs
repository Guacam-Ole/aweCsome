namespace AweCsome.Buffer
{
    public class BufferFileMeta
    {
        public enum AttachmentTypes { Attachment, DocLib };
        public int Id { get; private set; }
        public int ParentId { get; set; }
        public string Listname { get; set; }
        public string Filename { get; set; }
        public AttachmentTypes AttachmentType { get; set; }
        public string Folder { get; set; }
        public object AdditionalInformation { get; set; }

        public void SetId(int id)
        {
            Id = id;
        }
    }
}
