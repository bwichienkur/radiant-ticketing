using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace EnhancementHub.Api.Extensions;

/// <summary>
/// Duplicates controller routes under /api/v1/* for versioned clients while keeping /api/* for compatibility.
/// </summary>
public sealed class ApiV1RouteConvention : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            foreach (var selector in controller.Selectors.ToList())
            {
                var template = selector.AttributeRouteModel?.Template;
                if (template is null || !template.StartsWith("api/", StringComparison.Ordinal))
                {
                    continue;
                }

                if (template.StartsWith("api/v1/", StringComparison.Ordinal))
                {
                    continue;
                }

                controller.Selectors.Add(new SelectorModel(selector)
                {
                    AttributeRouteModel = new AttributeRouteModel
                    {
                        Template = template.Replace("api/", "api/v1/", StringComparison.Ordinal),
                    },
                });
            }
        }
    }
}

public static class ApiVersioningExtensions
{
    public static IMvcBuilder AddEnhancementHubApiVersioning(this IMvcBuilder mvcBuilder) =>
        mvcBuilder.AddMvcOptions(options => options.Conventions.Add(new ApiV1RouteConvention()));
}
