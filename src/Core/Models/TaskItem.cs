namespace DhCodetaskExtension.Core.Models
{
    public class TaskItem
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string[] Labels { get; set; } = new string[0];
        public string Url { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public string Repo { get; set; } = string.Empty;
    }
}
