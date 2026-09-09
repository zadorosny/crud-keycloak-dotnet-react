using System.ComponentModel.DataAnnotations;

namespace MfaCrud.Api.Products;

public record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int Stock,
    Guid CreatedById,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public class ProductRequest
{
    [Required]
    [MaxLength(120)]
    public string Name { get; init; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; init; }

    [Range(0, 9_999_999_999.99)]
    public decimal Price { get; init; }

    [Range(0, int.MaxValue)]
    public int Stock { get; init; }
}
