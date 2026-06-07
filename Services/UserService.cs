using AutoMapper;
using BCrypt.Net;
using DeviceMaintenanceSystem.Data;
using DeviceMaintenanceSystem.Dtos;
using DeviceMaintenanceSystem.Helpers;
using DeviceMaintenanceSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace DeviceMaintenanceSystem.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IJwtHelper _jwtHelper;
    private readonly IMapper _mapper;

    public AuthService(AppDbContext context, IJwtHelper jwtHelper, IMapper mapper)
    {
        _context = context;
        _jwtHelper = jwtHelper;
        _mapper = mapper;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("用户名或密码错误");
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("用户名或密码错误");
        }

        var token = _jwtHelper.GenerateToken(user.Id, user.Username, user.Role.ToString());
        var userDto = _mapper.Map<UserDto>(user);

        return new LoginResponse
        {
            Token = token,
            User = userDto
        };
    }

    public async Task<UserDto> GetCurrentUserAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("用户不存在");
        return _mapper.Map<UserDto>(user);
    }
}

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly IMapper _mapper;

    public UserService(AppDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<PagedResult<UserDto>> GetPagedAsync(UserQueryDto query)
    {
        var queryable = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchKeyword))
        {
            var keyword = query.SearchKeyword.ToLower();
            queryable = queryable.Where(u =>
                u.Username.ToLower().Contains(keyword) ||
                u.RealName.ToLower().Contains(keyword) ||
                u.Email.ToLower().Contains(keyword) ||
                u.Phone.Contains(keyword));
        }

        if (query.Role.HasValue)
        {
            queryable = queryable.Where(u => u.Role == query.Role.Value);
        }

        if (query.IsActive.HasValue)
        {
            queryable = queryable.Where(u => u.IsActive == query.IsActive.Value);
        }

        var totalCount = await queryable.CountAsync();

        var sortBy = query.SortBy?.ToLower() ?? "id";
        queryable = sortBy switch
        {
            "username" => query.SortDesc ? queryable.OrderByDescending(u => u.Username) : queryable.OrderBy(u => u.Username),
            "realname" => query.SortDesc ? queryable.OrderByDescending(u => u.RealName) : queryable.OrderBy(u => u.RealName),
            "role" => query.SortDesc ? queryable.OrderByDescending(u => u.Role) : queryable.OrderBy(u => u.Role),
            "createdat" => query.SortDesc ? queryable.OrderByDescending(u => u.CreatedAt) : queryable.OrderBy(u => u.CreatedAt),
            _ => query.SortDesc ? queryable.OrderByDescending(u => u.Id) : queryable.OrderBy(u => u.Id)
        };

        var items = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<UserDto>
        {
            Items = _mapper.Map<List<UserDto>>(items),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
        };
    }

    public async Task<UserDto?> GetByIdAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
        {
            throw new InvalidOperationException("用户名已存在");
        }

        var user = _mapper.Map<User>(dto);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return _mapper.Map<UserDto>(user);
    }

    public async Task<UserDto?> UpdateAsync(int id, UpdateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.RealName))
            user.RealName = dto.RealName;
        if (!string.IsNullOrWhiteSpace(dto.Email))
            user.Email = dto.Email;
        if (!string.IsNullOrWhiteSpace(dto.Phone))
            user.Phone = dto.Phone;
        if (dto.Role.HasValue)
            user.Role = dto.Role.Value;
        if (dto.IsActive.HasValue)
            user.IsActive = dto.IsActive.Value;

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return _mapper.Map<UserDto>(user);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        if (!BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("原密码错误");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
