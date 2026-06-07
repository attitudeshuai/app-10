using AutoMapper;
using DeviceMaintenanceSystem.Data;
using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DeviceMaintenanceSystem.Services;

public class SupplierRatingService : ISupplierRatingService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public SupplierRatingService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<SupplierRatingDto>> GetPagedAsync(SupplierRatingQueryDto query)
    {
        var queryable = _context.SupplierRatings
            .Include(r => r.Supplier)
            .Include(r => r.Rater)
            .Include(r => r.MaintenancePlan)
            .Include(r => r.FaultReport)
            .AsQueryable();

        if (query.SupplierId > 0)
        {
            queryable = queryable.Where(r => r.SupplierId == query.SupplierId);
        }

        if (query.WorkType.HasValue)
        {
            queryable = queryable.Where(r => r.WorkType == query.WorkType.Value);
        }

        if (query.MinScore.HasValue)
        {
            queryable = queryable.Where(r => r.Score >= query.MinScore.Value);
        }

        if (query.MaxScore.HasValue)
        {
            queryable = queryable.Where(r => r.Score <= query.MaxScore.Value);
        }

        var totalCount = await queryable.CountAsync();

        var sortBy = query.SortBy?.ToLower() ?? "createdat";
        queryable = sortBy switch
        {
            "score" => query.SortDesc ? queryable.OrderByDescending(r => r.Score) : queryable.OrderBy(r => r.Score),
            "worktype" => query.SortDesc ? queryable.OrderByDescending(r => r.WorkType) : queryable.OrderBy(r => r.WorkType),
            _ => query.SortDesc ? queryable.OrderByDescending(r => r.CreatedAt) : queryable.OrderBy(r => r.CreatedAt)
        };

        var items = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var dtos = _mapper.Map<List<SupplierRatingDto>>(items);

        return new PagedResult<SupplierRatingDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<SupplierRatingDto?> GetByIdAsync(int id)
    {
        var rating = await _context.SupplierRatings
            .Include(r => r.Supplier)
            .Include(r => r.Rater)
            .Include(r => r.MaintenancePlan)
            .Include(r => r.FaultReport)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rating == null) return null;

        return _mapper.Map<SupplierRatingDto>(rating);
    }

    public async Task<SupplierRatingDto> CreateAsync(CreateSupplierRatingDto dto, int raterId)
    {
        if (dto.Score < 1 || dto.Score > 5)
        {
            throw new InvalidOperationException("评分必须在 1-5 分之间");
        }

        var supplier = await _context.Suppliers.FindAsync(dto.SupplierId);
        if (supplier == null)
        {
            throw new InvalidOperationException("供应商不存在");
        }

        if (dto.WorkType == RatingWorkType.Maintenance && dto.MaintenancePlanId.HasValue)
        {
            var plan = await _context.MaintenancePlans.FindAsync(dto.MaintenancePlanId.Value);
            if (plan == null)
            {
                throw new InvalidOperationException("保养计划不存在");
            }
            if (plan.Status != MaintenancePlanStatus.Completed)
            {
                throw new InvalidOperationException("只能对已完成的保养工作进行评分");
            }
        }

        if (dto.WorkType == RatingWorkType.Repair && dto.FaultReportId.HasValue)
        {
            var report = await _context.FaultReports.FindAsync(dto.FaultReportId.Value);
            if (report == null)
            {
                throw new InvalidOperationException("故障报告不存在");
            }
            if (report.Status != FaultStatus.Completed)
            {
                throw new InvalidOperationException("只能对已完成的维修工作进行评分");
            }
        }

        var rating = _mapper.Map<SupplierRating>(dto);
        rating.RaterId = raterId;
        rating.CreatedAt = DateTime.UtcNow;

        _context.SupplierRatings.Add(rating);
        await _context.SaveChangesAsync();

        var result = await GetByIdAsync(rating.Id);
        return result!;
    }

    public async Task<bool> DeleteAsync(int id, int userId)
    {
        var rating = await _context.SupplierRatings.FindAsync(id);
        if (rating == null) return false;

        if (rating.RaterId != userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || user.Role != UserRole.Admin)
            {
                throw new InvalidOperationException("只能删除自己的评分，或由管理员删除");
            }
        }

        _context.SupplierRatings.Remove(rating);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<SupplierRatingSummaryDto> GetSummaryAsync(int supplierId)
    {
        var ratings = await _context.SupplierRatings
            .Where(r => r.SupplierId == supplierId)
            .Select(r => r.Score)
            .ToListAsync();

        var summary = new SupplierRatingSummaryDto
        {
            SupplierId = supplierId,
            RatingCount = ratings.Count,
            AverageRating = ratings.Count > 0 ? Math.Round(ratings.Average(), 2) : 0,
            OneStarCount = ratings.Count(s => s == 1),
            TwoStarCount = ratings.Count(s => s == 2),
            ThreeStarCount = ratings.Count(s => s == 3),
            FourStarCount = ratings.Count(s => s == 4),
            FiveStarCount = ratings.Count(s => s == 5)
        };

        return summary;
    }

    public async Task<List<SupplierRatingDto>> GetSupplierRatingsAsync(int supplierId, int limit = 10)
    {
        var ratings = await _context.SupplierRatings
            .Include(r => r.Rater)
            .Include(r => r.MaintenancePlan)
            .Include(r => r.FaultReport)
            .Where(r => r.SupplierId == supplierId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return _mapper.Map<List<SupplierRatingDto>>(ratings);
    }
}
