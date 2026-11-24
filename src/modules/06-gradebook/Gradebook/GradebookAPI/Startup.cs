// ReSharper disable RedundantTypeArgumentsOfMethod

using System.Net.Mime;
using GradebookAPI.Data;
using GradebookAPI.Services;
using GradebookAPI.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GradebookAPI;

public static class Startup
{
    extension(IServiceCollection serviceCollection)
    {
        public IServiceCollection ConfigureServices(IConfiguration configuration)
        {
            serviceCollection
                .AddSingleton<TimeProvider>(TimeProvider.System)
                .AddScoped<IGradeCalculationService, GradeCalculationService>();

            // Register the SQLite database context.
            serviceCollection.AddDbContext<GradebookDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("GradebookDatabase")));

            return serviceCollection;
        }
    }

    extension(IMvcBuilder mvcBuilder)
    {
        public IMvcBuilder ConfigureControllers()
        {
            return mvcBuilder.ConfigureApiBehaviorOptions(options =>
            {
                // Force internal ASP.NET validation errors to return as strings instead of ProblemDetails
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage);

                    // Preserve the original status code if set, otherwise default to 400
                    // ASP.NET defaults to 200 until the controller executes
                    var statusCode = context.HttpContext.Response.StatusCode != StatusCodes.Status200OK
                        ? context.HttpContext.Response.StatusCode
                        : StatusCodes.Status400BadRequest;

                    if (context.HttpContext.Request.Headers.Accept == MediaTypeNames.Text.Plain)
                    {
                        return new ContentResult
                        {
                            Content = string.Join(" ", errors),
                            ContentType = MediaTypeNames.Text.Plain,
                            StatusCode = statusCode
                        };
                    }

                    return new ContentResult
                    {
                        Content = $"\"{string.Join(" ", errors)}\"",
                        ContentType = MediaTypeNames.Application.Json,
                        StatusCode = statusCode
                    };
                };
            });
        }
    }
}