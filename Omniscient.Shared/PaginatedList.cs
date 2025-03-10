namespace Omniscient.Shared;

public class PaginatedList<T>
{
    public int PageIndex  { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public List<T> Items { get; set; }
    
    public PaginatedList(List<T> items, int totalCount, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        Items = items;
    }
}