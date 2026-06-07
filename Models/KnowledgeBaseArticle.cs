namespace DeviceMaintenanceSystem.Models;

public enum KnowledgeBaseStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

public class KnowledgeBaseArticle
{
    public int Id { get; set; }
    public string ArticleCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Keywords { get; set; }
    public int DeviceId { get; set; }
    public Device? Device { get; set; }
    public int AuthorId { get; set; }
    public User? Author { get; set; }
    public KnowledgeBaseStatus Status { get; set; } = KnowledgeBaseStatus.Draft;
    public int ViewCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<KnowledgeBaseArticleTag> ArticleTags { get; set; } = new List<KnowledgeBaseArticleTag>();
}
