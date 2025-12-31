var builder = WebApplication.CreateBuilder(args);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

var app = builder.Build();

// Use CORS
app.UseCors("AllowBlazorClient");

app.MapGet("/api/products", () =>

{

    return new[]

    {

        new { Id = 1, Name = "Laptop", Price = 1200.50, Stock = 25 },

        new { Id = 2, Name = "Headphones", Price = 50.00, Stock = 100 }

    };

});

app.Run();
