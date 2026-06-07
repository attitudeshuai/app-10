namespace DeviceMaintenanceSystem.Dtos;

public class KnowledgeBaseArticleDto
{
    public int Id { get; set; }
    public string ArticleCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Keywords { get; set; }
    public int DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceCode { get; set; }
    public string? DeviceCategory { get; set; }
    public int AuthorId { get; set; }
    public string? AuthorName { get; set; }
    public KnowledgeBaseStatus Status { get; set; }
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class KnowledgeBaseArticleBriefDto
{
    public int Id { get; set; }
    public string ArticleCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? Keywords { get; set; }
    public int DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceCategory { get; set; }
    public string? AuthorName { get; set; }
    public KnowledgeBaseStatus Status { get; set; }
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateKnowledgeBaseArticleDto
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Keywords { get; set; }
    public int DeviceId { get; set; }
    public KnowledgeBaseStatus Status { get; set; } = KnowledgeBaseStatus.Draft;
}

public class UpdateKnowledgeBaseArticleDto
{
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public string? Content { get; set; }
    public string? Keywords { get; set; }
    public int? DeviceId { get; set; }
    public KnowledgeBaseStatus? Status { get; set; }
}

public class KnowledgeBaseArticleQueryDto : PagedQuery
{
    public int? DeviceId { get; set; }
    public string? DeviceCategory { get; set; }
    public int? AuthorId { get; set; }
    public KnowledgeBaseStatus? Status { get; set; }
}

public class KnowledgeBaseStatisticsDto
{
    public int TotalCount { get; set; }
    public int PublishedCount { get; set; }
    public int DraftCount { get; set; }
    public int ArchivedCount { get; set; }
    public int TotalViewCount { get; set; }
    public List<KnowledgeBaseArticleBriefDto>? TopViewedArticles { get; set; }
    public List<KnowledgeBaseCategoryStatDto>? CategoryStats { get; set; }
}

public class KnowledgeBaseCategoryStatDto
{
    public string Category { get; set; } = string.Empty;
    public int ArticleCount { get; set; }
}
