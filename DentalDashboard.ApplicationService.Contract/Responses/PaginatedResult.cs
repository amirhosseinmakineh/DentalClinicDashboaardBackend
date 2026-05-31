using System;
using System.Collections.Generic;
using System.Text;

namespace DentalDashboard.ApplicationService.Contract.Responses
{
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = [];

        public int TotalCount { get; set; }

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public int TotalPages =>
            (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}
