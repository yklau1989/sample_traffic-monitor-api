using System.ComponentModel.DataAnnotations;
using TrafficMonitor.Api.Middleware;
using TrafficMonitor.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// TODO(#17): remove smoke routes when controller lands
app.MapGet("/__smoke/validation", () =>
{
    throw new ValidationException(new ValidationResult("name is required", ["name"]), null, null);
});

app.MapGet("/__smoke/boom", () =>
{
    throw new InvalidOperationException("forced 500 for demo");
});

app.Run();

public partial class Program
{
}
