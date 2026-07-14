using Microsoft.EntityFrameworkCore;

namespace NomoAI.API.Common.Abstractions
{
    public class PaginatedList<T>(List<T> items, int pageNumber, int count, int pageSize)
    {

        public List<T> Items { get; private set; } = items;

        public int PageNumber { get; private set; } = pageNumber;

        public int TotalPages { get; private set; } = (int)Math.Ceiling(count / (double)pageSize);

        public bool HasPreviousPage => PageNumber > 1;

        public bool HasNextPage => PageNumber < TotalPages;

        public static async Task<PaginatedList<T>>CreateAsync(IQueryable<T> source , 
            int pageNumber  , int pageSize)
        {
            var count = await source.CountAsync();

            var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

            return new PaginatedList<T>(items, pageNumber, count, pageSize);
           
        }
    }
}
