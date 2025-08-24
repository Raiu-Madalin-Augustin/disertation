namespace MiniShop.API.dto;

public class PlaceOrderResponseDto
{
    public int OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ItemsCount { get; set; }
    public decimal Total { get; set; }
}