namespace Omniscient.Shared.Mappers;

public static class PaginatedListMapper
{
    public static PaginatedList<TDestination> MapTo<TSource, TDestination>(
        this PaginatedList<TSource> source,
        Func<TSource, TDestination> mapper)
    {
        var mappedItems = source.Items.Select(mapper).ToList();
        return new PaginatedList<TDestination>(
            mappedItems, 
            source.TotalCount, 
            source.PageIndex, 
            source.PageSize);
    }
}