namespace DeviceMaintenanceSystem.Dtos;

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new List<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class PagedQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchKeyword { get; set; }
    public string? SortBy { get; set; }
    public bool SortDesc { get; set; } = false;
}
