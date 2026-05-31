namespace DentalDashboard.Framwork.Domain;

public class PaginatedRequest
{
    private const int MaxPageSize = 100;

    private int _pageSize = 10;

    public int PageNumber { get; set; } = 1;

    public int PageSize
    {
        get => _pageSize;

        set
        {
            if (value <= 0)
            {
                _pageSize = 10;

                return;
            }
            _pageSize =
                value > MaxPageSize
                    ? MaxPageSize
                    : value;
        }
    }
}
