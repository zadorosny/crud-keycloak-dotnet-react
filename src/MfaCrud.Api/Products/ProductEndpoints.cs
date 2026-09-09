using System.Security.Claims;
using MfaCrud.Api.Auth;
using MfaCrud.Api.Common;
using MfaCrud.Api.Data;
using MfaCrud.Api.Models;
using MfaCrud.Api.Telemetry;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace MfaCrud.Api.Products;

public static class ProductEndpoints
{
    private const int MaxPageSize = 100;

    public static RouteGroupBuilder MapProductEndpoints(this RouteGroupBuilder group)
    {
        var products = group.MapGroup("/products").WithTags("Products").RequireAuthorization();

        products.MapGet("", GetProducts).WithName("GetProducts");
        products.MapGet("/{id:guid}", GetProduct).WithName("GetProduct");
        products.MapPost("", CreateProduct).WithName("CreateProduct").RequireAuthorization(AuthenticationExtensions.StaffOrAdmin);
        products.MapPut("/{id:guid}", UpdateProduct).WithName("UpdateProduct").RequireAuthorization(AuthenticationExtensions.StaffOrAdmin);
        products.MapDelete("/{id:guid}", DeleteProduct).WithName("DeleteProduct").RequireAuthorization(AuthenticationExtensions.AdminOnly);

        return group;
    }

    private static async Task<Ok<PagedResult<ProductDto>>> GetProducts(
        AppDbContext db, int page = 1, int pageSize = 20, string? search = null, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var query = db.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto(p.Id, p.Name, p.Description, p.Price, p.Stock, p.CreatedById, p.CreatedAt, p.UpdatedAt))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(new PagedResult<ProductDto>(items, page, pageSize, total));
    }

    private static async Task<Results<Ok<ProductDto>, NotFound>> GetProduct(
        Guid id, AppDbContext db, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return product is null ? TypedResults.NotFound() : TypedResults.Ok(ToDto(product));
    }

    private static async Task<Created<ProductDto>> CreateProduct(
        ProductRequest request, ClaimsPrincipal user, AppDbContext db, MfaCrudTelemetry telemetry, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Stock = request.Stock,
            CreatedById = user.GetUserId(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken);
        telemetry.ProductWritten("created");

        return TypedResults.Created($"/api/v1/products/{product.Id}", ToDto(product));
    }

    private static async Task<Results<Ok<ProductDto>, NotFound>> UpdateProduct(
        Guid id, ProductRequest request, AppDbContext db, MfaCrudTelemetry telemetry, CancellationToken cancellationToken)
    {
        var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product is null)
        {
            return TypedResults.NotFound();
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Stock = request.Stock;
        product.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        telemetry.ProductWritten("updated");

        return TypedResults.Ok(ToDto(product));
    }

    private static async Task<Results<NoContent, NotFound>> DeleteProduct(
        Guid id, AppDbContext db, MfaCrudTelemetry telemetry, CancellationToken cancellationToken)
    {
        var deleted = await db.Products.Where(p => p.Id == id).ExecuteDeleteAsync(cancellationToken);
        if (deleted == 0)
        {
            return TypedResults.NotFound();
        }

        telemetry.ProductWritten("deleted");
        return TypedResults.NoContent();
    }

    private static ProductDto ToDto(Product p) =>
        new(p.Id, p.Name, p.Description, p.Price, p.Stock, p.CreatedById, p.CreatedAt, p.UpdatedAt);
}
