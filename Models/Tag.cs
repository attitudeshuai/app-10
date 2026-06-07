namespace DeviceMaintenanceSystem.Models;

public enum TagType
{
    FaultType = 0,
    DeviceCategory = 1,
    Custom = 2
}

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TagType Type { get; set; } = TagType.Custom;
    public string? Color { get; set; }
    public int SortOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<KnowledgeBaseArticleTag> ArticleTags { get; set; } = new List<KnowledgeBaseArticleTag>();
}

public class KnowledgeBaseArticleTag
{
    public int ArticleId { get; set; }
    public KnowledgeBaseArticle? Article { get; set; }

    public int TagId { get; set; }
    public Tag? Tag { get; set; }
}
