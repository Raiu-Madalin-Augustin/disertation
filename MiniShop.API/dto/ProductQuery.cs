namespace MiniShop.API.dto;

public class ProductQuery
{
    public int? CategoryId { get; set; }
    public string? Search { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? SortBy { get; set; } = "name";   // name|price|stock
    public string? SortDir { get; set; } = "asc";   // asc|desc
    public int Page { get; set; } = 1;              // 1-based
    public int PageSize { get; set; } = 10;
}
