namespace ImageUploader
{
    public class Image
    {
        public string Id { get; set; }
        public string? Title { get; set; }
        public string? ImagePath { get; set; }
        public Image()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
