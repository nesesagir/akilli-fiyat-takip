using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PriceTracker.API;

/// <summary>
/// Swagger Authorize (AdminToken) bilgisini /api/admin/users ve logout isteklerine ekler.
/// </summary>
public sealed class AdminTokenOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath ?? "";
        if (!path.StartsWith("api/admin", StringComparison.OrdinalIgnoreCase))
            return;

        // login ve status token istemez
        if (path.Contains("login", StringComparison.OrdinalIgnoreCase) ||
            path.Contains("status", StringComparison.OrdinalIgnoreCase))
            return;

        operation.Security ??= new List<OpenApiSecurityRequirement>();
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "AdminToken"
                    }
                },
                Array.Empty<string>()
            }
        });
    }
}
