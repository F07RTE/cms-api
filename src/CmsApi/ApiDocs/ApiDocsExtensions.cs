using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

namespace CmsApi.ApiDocs;

public static class ApiDocsExtensions
{
    public static IServiceCollection AddCmsApiDocs(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<ApiDocsOptions>()
            .Bind(configuration.GetSection(ApiDocsOptions.SectionName));
        services.AddOpenApi(options =>
            options
                .AddDocumentTransformer<BasicSecuritySchemeTransformer>()
                .AddOperationTransformer<BatchRequestBodyTransformer>()
        );
        return services;
    }

    public static WebApplication MapCmsApiDocs(this WebApplication app)
    {
        if (!app.Services.GetRequiredService<IOptions<ApiDocsOptions>>().Value.Enabled)
        {
            return app;
        }

        app.MapOpenApi().AllowAnonymous();
        app.MapScalarApiReference().AllowAnonymous();
        return app;
    }
}
