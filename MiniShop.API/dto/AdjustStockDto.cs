namespace MiniShop.API.dto;

public class AdjustStockDto
{
    public int ProductId { get; set; }
    public int Delta { get; set; } // poate fi negativ (scădere) sau pozitiv (creștere)
}