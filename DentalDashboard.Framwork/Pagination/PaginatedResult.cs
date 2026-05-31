namespace DentalDashboard.Domain.Models;

public class PaginatedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();

    public int TotalCount { get; set; }

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);

    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}