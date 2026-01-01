using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;

var builder = WebApplication.CreateBuilder(args);

// Add CORS
builder.Services.AddCors();

// Add Memory Cache for performance optimization
builder.Services.AddMemoryCache();

// Add Response Compression for reduced bandwidth
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
});

// Configure JSON serialization options to follow industry standards
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    options.SerializerOptions.WriteIndented = false; // Minimize payload size in production
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

// Use Response Compression
app.UseResponseCompression();

// Use CORS
app.UseCors(policy =>
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader());

// Get cache service
var cache = app.Services.GetRequiredService<IMemoryCache>();
const string ProductsCacheKey = "products_cache";
var cacheOptions = new MemoryCacheEntryOptions()
    .SetAbsoluteExpiration(TimeSpan.FromMinutes(10))
    .SetSlidingExpiration(TimeSpan.FromMinutes(3));

// Helper method to get or create products
List<Product> GetProducts()
{
    return cache.GetOrCreate(ProductsCacheKey, entry =>
    {
        entry.SetOptions(cacheOptions);
        Console.WriteLine("Cache miss - generating product list");
        
        return new List<Product>
        {
            new Product
            {
                Id = 1,
                Name = "Laptop",
                Description = "High-performance laptop for developers",
                Sku = "LAP-001",
                Price = 1200.50m,
                Stock = 25,
                CategoryId = 101,
                Category = new Category { Id = 101, Name = "Electronics" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new Product
            {
                Id = 2,
                Name = "Headphones",
                Description = "Noise-cancelling wireless headphones",
                Sku = "HEAD-002",
                Price = 50.00m,
                Stock = 100,
                CategoryId = 102,
                Category = new Category { Id = 102, Name = "Accessories" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            }
        };
    }) ?? new List<Product>();
}

// GET /api/products - RESTful endpoint with proper naming
app.MapGet("/api/products", () =>
{
    var products = GetProducts();

    var response = new ApiResponse<List<Product>>
    {
        Success = true,
        Data = products,
        Message = "Products retrieved successfully",
        Timestamp = DateTime.UtcNow,
        Count = products.Count
    };

    return Results.Ok(response);
})
.WithName("GetProducts")
.Produces<ApiResponse<List<Product>>>(200)
.Produces(500);

// Legacy endpoint for backward compatibility - uses same cached data
app.MapGet("/api/productlist", () =>
{
    var products = GetProducts();
    
    // Map to legacy format
    return products.Select(p => new
    {
        p.Id,
        p.Name,
        Price = (double)p.Price,
        p.Stock,
        Category = p.Category != null ? new { p.Category.Id, p.Category.Name } : null
    }).ToArray();
});

app.Run();

// Models following industry standards
public class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Sku { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
}

// Standard API response wrapper
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; }
    public int? Count { get; set; }
    public List<string>? Errors { get; set; }
}
