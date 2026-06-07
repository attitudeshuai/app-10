using AutoMapper;
using DeviceMaintenanceSystem.Data;
using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DeviceMaintenanceSystem.Services;

public class KnowledgeBaseService : IKnowledgeBaseService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public KnowledgeBaseService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<KnowledgeBaseArticleBriefDto>> GetPagedAsync(KnowledgeBaseArticleQueryDto query)
    {
        var queryable = _context.KnowledgeBaseArticles
            .Include(a => a.Device)
            .Include(a => a.Author)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
        {
            var keyword = query.SearchKeyword.ToLower();
            queryable = queryable.Where(a =>
                a.Title.ToLower().Contains(keyword) ||
                a.Summary.ToLower().Contains(keyword) ||
                a.Content.ToLower().Contains(keyword) ||
                (a.Keywords != null && a.Keywords.ToLower().Contains(keyword)));
        }

        if (query.DeviceId.HasValue)
            queryable = queryable.Where(a => a.DeviceId == query.DeviceId.Value);

        if (!string.IsNullOrWhiteSpace(query.DeviceCategory))
        {
            var category = query.DeviceCategory.Trim();
            queryable = queryable.Where(a => a.Device != null && a.Device.Category == category);
        }

        if (query.AuthorId.HasValue)
            queryable = queryable.Where(a => a.AuthorId == query.AuthorId.Value);

        if (query.Status.HasValue)
            queryable = queryable.Where(a => a.Status == query.Status.Value);

        var totalCount = await queryable.CountAsync();

        var sortBy = query.SortBy?.ToLower() ?? "id";
        queryable = sortBy switch
        {
            "title" => query.SortDesc ? queryable.OrderByDescending(a => a.Title) : queryable.OrderBy(a => a.Title),
            "viewcount" => query.SortDesc ? queryable.OrderByDescending(a => a.ViewCount) : queryable.OrderBy(a => a.ViewCount),
            "createdat" => query.SortDesc ? queryable.OrderByDescending(a => a.CreatedAt) : queryable.OrderBy(a => a.CreatedAt),
            "updatedat" => query.SortDesc ? queryable.OrderByDescending(a => a.UpdatedAt) : queryable.OrderBy(a => a.UpdatedAt),
            _ => query.SortDesc ? queryable.OrderByDescending(a => a.Id) : queryable.OrderBy(a => a.Id)
        };

        var items = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<KnowledgeBaseArticleBriefDto>>(items);
        return new PagedResult<KnowledgeBaseArticleBriefDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<KnowledgeBaseArticleDto?> GetByIdAsync(int id)
    {
        var article = await _context.KnowledgeBaseArticles
            .Include(a => a.Device)
            .Include(a => a.Author)
            .FirstOrDefaultAsync(a => a.Id == id);
        return article == null ? null : _mapper.Map<KnowledgeBaseArticleDto>(article);
    }

    public async Task<KnowledgeBaseArticleDto> CreateAsync(CreateKnowledgeBaseArticleDto dto, int authorId)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
        {
            throw new InvalidOperationException("文章标题不能为空");
        }

        if (string.IsNullOrWhiteSpace(dto.Content))
        {
            throw new InvalidOperationException("文章内容不能为空");
        }

        var device = await _context.Devices.FindAsync(dto.DeviceId);
        if (device == null)
        {
            throw new KeyNotFoundException("设备不存在");
        }

        var author = await _context.Users.FindAsync(authorId);
        if (author == null)
        {
            throw new KeyNotFoundException("作者不存在");
        }

        var article = _mapper.Map<KnowledgeBaseArticle>(dto);
        article.AuthorId = authorId;
        article.ArticleCode = await GenerateArticleCodeAsync();
        article.CreatedAt = DateTime.UtcNow;
        article.UpdatedAt = DateTime.UtcNow;

        _context.KnowledgeBaseArticles.Add(article);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(article.Id) ?? _mapper.Map<KnowledgeBaseArticleDto>(article);
    }

    public async Task<KnowledgeBaseArticleDto?> UpdateAsync(int id, UpdateKnowledgeBaseArticleDto dto)
    {
        var article = await _context.KnowledgeBaseArticles.FindAsync(id);
        if (article == null) return null;

        if (dto.Title != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
            {
                throw new InvalidOperationException("文章标题不能为空");
            }
            article.Title = dto.Title.Trim();
        }
        if (dto.Summary != null)
            article.Summary = dto.Summary;
        if (dto.Content != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Content))
            {
                throw new InvalidOperationException("文章内容不能为空");
            }
            article.Content = dto.Content;
        }
        if (dto.Keywords != null)
            article.Keywords = dto.Keywords;
        if (dto.DeviceId.HasValue)
        {
            var device = await _context.Devices.FindAsync(dto.DeviceId.Value);
            if (device == null)
            {
                throw new KeyNotFoundException("设备不存在");
            }
            article.DeviceId = dto.DeviceId.Value;
        }
        if (dto.Status.HasValue)
            article.Status = dto.Status.Value;

        article.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var article = await _context.KnowledgeBaseArticles.FindAsync(id);
        if (article == null) return false;

        _context.KnowledgeBaseArticles.Remove(article);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<KnowledgeBaseStatisticsDto> GetStatisticsAsync()
    {
        var total = await _context.KnowledgeBaseArticles.CountAsync();
        var published = await _context.KnowledgeBaseArticles.CountAsync(a => a.Status == KnowledgeBaseStatus.Published);
        var draft = await _context.KnowledgeBaseArticles.CountAsync(a => a.Status == KnowledgeBaseStatus.Draft);
        var archived = await _context.KnowledgeBaseArticles.CountAsync(a => a.Status == KnowledgeBaseStatus.Archived);
        var totalViews = await _context.KnowledgeBaseArticles.SumAsync(a => a.ViewCount);

        var topViewed = await _context.KnowledgeBaseArticles
            .Include(a => a.Device)
            .Include(a => a.Author)
            .Where(a => a.Status == KnowledgeBaseStatus.Published)
            .OrderByDescending(a => a.ViewCount)
            .Take(10)
            .ToListAsync();

        var categoryStats = await _context.KnowledgeBaseArticles
            .Include(a => a.Device)
            .Where(a => a.Status == KnowledgeBaseStatus.Published && a.Device != null)
            .GroupBy(a => a.Device!.Category)
            .Select(g => new KnowledgeBaseCategoryStatDto
            {
                Category = g.Key,
                ArticleCount = g.Count()
            })
            .OrderByDescending(g => g.ArticleCount)
            .ToListAsync();

        return new KnowledgeBaseStatisticsDto
        {
            TotalCount = total,
            PublishedCount = published,
            DraftCount = draft,
            ArchivedCount = archived,
            TotalViewCount = totalViews,
            TopViewedArticles = _mapper.Map<List<KnowledgeBaseArticleBriefDto>>(topViewed),
            CategoryStats = categoryStats
        };
    }

    public async Task<KnowledgeBaseArticleDto?> IncrementViewCountAsync(int id)
    {
        var article = await _context.KnowledgeBaseArticles.FindAsync(id);
        if (article == null) return null;

        article.ViewCount++;
        article.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task<List<KnowledgeBaseArticleBriefDto>> GetRecommendedArticlesByDeviceIdAsync(int deviceId, int limit = 5)
    {
        var device = await _context.Devices
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == deviceId);

        if (device == null)
            return new List<KnowledgeBaseArticleBriefDto>();

        var deviceCategory = device.Category;

        var articles = await _context.KnowledgeBaseArticles
            .Include(a => a.Device)
            .Include(a => a.Author)
            .Where(a => a.Status == KnowledgeBaseStatus.Published)
            .Where(a => a.Device != null && a.Device.Category == deviceCategory)
            .OrderByDescending(a => a.DeviceId == deviceId ? 1 : 0)
            .ThenByDescending(a => a.ViewCount)
            .ThenByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return _mapper.Map<List<KnowledgeBaseArticleBriefDto>>(articles);
    }

    private async Task<string> GenerateArticleCodeAsync()
    {
        var datePrefix = DateTime.Now.ToString("yyyyMMdd");
        var count = await _context.KnowledgeBaseArticles
            .CountAsync(a => a.ArticleCode.StartsWith("KB-" + datePrefix));
        return $"KB-{datePrefix}-{(count + 1).ToString("D4")}";
    }
}
