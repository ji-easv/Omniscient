﻿namespace Omniscient.Shared;

public class PaginatedList<T>
{
    public int PageIndex  { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }
    
    public PaginatedList(List<T> items, int totalCount, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalCount = totalCount;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        Items = items;
    }

    public static PaginatedList<T> Empty()
    {
        return new PaginatedList<T>([], 0, 1, 10);
    }
}