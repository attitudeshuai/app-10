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
            .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
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

        if (query.TagIds != null && query.TagIds.Count > 0)
        {
            foreach (var tagId in query.TagIds)
            {
                queryable = queryable.Where(a => a.ArticleTags.Any(at => at.TagId == tagId));
            }
        }

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
            .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
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

        if (dto.TagIds != null && dto.TagIds.Count > 0)
        {
            var tags = await _context.Tags
                .Where(t => dto.TagIds.Contains(t.Id))
                .ToListAsync();

            if (tags.Count != dto.TagIds.Count)
            {
                throw new KeyNotFoundException("部分标签不存在");
            }

            article.ArticleTags = tags.Select(t => new KnowledgeBaseArticleTag
            {
                TagId = t.Id,
                Article = article
            }).ToList();
        }

        _context.KnowledgeBaseArticles.Add(article);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(article.Id) ?? _mapper.Map<KnowledgeBaseArticleDto>(article);
    }

    public async Task<KnowledgeBaseArticleDto?> UpdateAsync(int id, UpdateKnowledgeBaseArticleDto dto)
    {
        var article = await _context.KnowledgeBaseArticles
            .Include(a => a.ArticleTags)
            .FirstOrDefaultAsync(a => a.Id == id);
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

        if (dto.TagIds != null)
        {
            var existingTagIds = article.ArticleTags.Select(at => at.TagId).ToList();
            var newTagIds = dto.TagIds.Distinct().ToList();

            var tagsToRemove = existingTagIds.Except(newTagIds).ToList();
            var tagsToAdd = newTagIds.Except(existingTagIds).ToList();

            if (tagsToRemove.Count > 0)
            {
                var tagsToRemoveEntities = article.ArticleTags
                    .Where(at => tagsToRemove.Contains(at.TagId))
                    .ToList();
                _context.KnowledgeBaseArticleTags.RemoveRange(tagsToRemoveEntities);
            }

            if (tagsToAdd.Count > 0)
            {
                var validTags = await _context.Tags
                    .Where(t => tagsToAdd.Contains(t.Id))
                    .ToListAsync();

                if (validTags.Count != tagsToAdd.Count)
                {
                    throw new KeyNotFoundException("部分标签不存在");
                }

                foreach (var tag in validTags)
                {
                    article.ArticleTags.Add(new KnowledgeBaseArticleTag
                    {
                        ArticleId = article.Id,
                        TagId = tag.Id
                    });
                }
            }
        }

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
            .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
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
            .Include(a => a.ArticleTags)
                .ThenInclude(at => at.Tag)
            .Where(a => a.Status == KnowledgeBaseStatus.Published)
            .Where(a => a.Device != null && a.Device.Category == deviceCategory)
            .OrderByDescending(a => a.DeviceId == deviceId ? 1 : 0)
            .ThenByDescending(a => a.ViewCount)
            .ThenByDescending(a => a.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return _mapper.Map<List<KnowledgeBaseArticleBriefDto>>(articles);
    }

    public async Task<PagedResult<TagDto>> GetTagsPagedAsync(TagQueryDto query)
    {
        var queryable = _context.Tags
            .Include(t => t.ArticleTags)
            .AsQueryable();

        if (query.Type.HasValue)
            queryable = queryable.Where(t => t.Type == query.Type.Value);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var keyword = query.Keyword.Trim().ToLower();
            queryable = queryable.Where(t => t.Name.ToLower().Contains(keyword));
        }

        var totalCount = await queryable.CountAsync();

        queryable = queryable
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name);

        var items = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<TagDto>>(items);
        return new PagedResult<TagDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<List<TagDto>> GetAllTagsAsync(TagType? type = null)
    {
        var queryable = _context.Tags
            .Include(t => t.ArticleTags)
            .AsQueryable();

        if (type.HasValue)
            queryable = queryable.Where(t => t.Type == type.Value);

        var tags = await queryable
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name)
            .ToListAsync();

        return _mapper.Map<List<TagDto>>(tags);
    }

    public async Task<TagDto?> GetTagByIdAsync(int id)
    {
        var tag = await _context.Tags
            .Include(t => t.ArticleTags)
            .FirstOrDefaultAsync(t => t.Id == id);
        return tag == null ? null : _mapper.Map<TagDto>(tag);
    }

    public async Task<TagDto> CreateTagAsync(CreateTagDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new InvalidOperationException("标签名称不能为空");
        }

        var existingTag = await _context.Tags
            .FirstOrDefaultAsync(t => t.Name == dto.Name.Trim());
        if (existingTag != null)
        {
            throw new InvalidOperationException("标签名称已存在");
        }

        var tag = _mapper.Map<Tag>(dto);
        tag.Name = tag.Name.Trim();
        tag.CreatedAt = DateTime.UtcNow;

        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();

        return _mapper.Map<TagDto>(tag);
    }

    public async Task<TagDto?> UpdateTagAsync(int id, UpdateTagDto dto)
    {
        var tag = await _context.Tags.FindAsync(id);
        if (tag == null) return null;

        if (dto.Name != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new InvalidOperationException("标签名称不能为空");
            }

            var trimmedName = dto.Name.Trim();
            var existingTag = await _context.Tags
                .FirstOrDefaultAsync(t => t.Name == trimmedName && t.Id != id);
            if (existingTag != null)
            {
                throw new InvalidOperationException("标签名称已存在");
            }

            tag.Name = trimmedName;
        }

        if (dto.Type.HasValue)
            tag.Type = dto.Type.Value;

        if (dto.Color != null)
            tag.Color = dto.Color;

        if (dto.SortOrder.HasValue)
            tag.SortOrder = dto.SortOrder.Value;

        await _context.SaveChangesAsync();

        return await GetTagByIdAsync(id);
    }

    public async Task<bool> DeleteTagAsync(int id)
    {
        var tag = await _context.Tags.FindAsync(id);
        if (tag == null) return false;

        _context.Tags.Remove(tag);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<string> GenerateArticleCodeAsync()
    {
        var datePrefix = DateTime.Now.ToString("yyyyMMdd");
        var count = await _context.KnowledgeBaseArticles
            .CountAsync(a => a.ArticleCode.StartsWith("KB-" + datePrefix));
        return $"KB-{datePrefix}-{(count + 1).ToString("D4")}";
    }
}
