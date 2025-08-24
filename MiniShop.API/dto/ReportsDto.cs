namespace MiniShop.API.dto;

public record LowStockItemDto(int Id, string Name, int Stock, decimal Price, int CategoryId, string CategoryName);
public record SalesByCategoryRowDto(int CategoryId, string CategoryName, int TotalQty, decimal TotalRevenue);
public record TopProductRowDto(int ProductId, string ProductName, int CategoryId, string CategoryName, int TotalQty, decimal TotalRevenue);
public record InventoryValuationRowDto(int CategoryId, string CategoryName, int DistinctProducts, int TotalQty, decimal Valuation);
public record AuditLogRowDto(int Id, string PerformedBy, string Action, DateTime Timestamp);
