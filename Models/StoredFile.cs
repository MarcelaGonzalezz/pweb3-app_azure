namespace FileShareApp.Models
{
    public class StoredFile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string OriginalName { get; set; } = "";
        public string BlobName { get; set; } = "";
        public string ContentType { get; set; } = "application/octet-stream";
        public long SizeBytes { get; set; }
        public string UploadedByUserId { get; set; } = "";
        public string UploadedByEmail { get; set; } = "";
        public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
