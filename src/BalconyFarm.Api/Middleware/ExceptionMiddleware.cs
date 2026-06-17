using BalconyFarm.Application.Models;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BalconyFarm.Api.Middleware;

public class ExceptionMiddleware : IMiddleware
{
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(ILogger<ExceptionMiddleware> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "发生未处理的异常");

        ApiResponse response;
        int statusCode;

        switch (exception)
        {
            case ArgumentException or ValidationException:
                statusCode = StatusCodes.Status400BadRequest;
                response = ApiResponse.Error(exception.Message, 400);
                break;
            case UnauthorizedAccessException:
                statusCode = StatusCodes.Status401Unauthorized;
                response = ApiResponse.Error(exception.Message, 401);
                break;
            case KeyNotFoundException or FileNotFoundException:
                statusCode = StatusCodes.Status404NotFound;
                response = ApiResponse.Error(exception.Message, 404);
                break;
            default:
                statusCode = StatusCodes.Status500InternalServerError;
                response = ApiResponse.Error("服务器内部错误", 500);
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return context.Response.WriteAsync(json);
    }
}
