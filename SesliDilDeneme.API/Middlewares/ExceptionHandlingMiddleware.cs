using System.Net;
using System.Text.Json;
using SesliDil.Core.Responses;

namespace SesliDilDeneme.API.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized");
                await WriteError(context, HttpStatusCode.Unauthorized, "Yetkisiz erişim.");
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogInformation(ex, "Not Found");
                await WriteError(context, HttpStatusCode.NotFound, "Kayıt bulunamadı.");
            }
            catch (ArgumentException ex)
            {
                _logger.LogInformation(ex, "Bad Request");
                await WriteError(context, HttpStatusCode.BadRequest, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled");
                await WriteError(context, HttpStatusCode.InternalServerError, "Beklenmeyen bir hata oluştu.");
            }
        }

        private static async Task WriteError(HttpContext ctx, HttpStatusCode code, string message)
        {
            ctx.Response.StatusCode = (int)code;
            ctx.Response.ContentType = "application/json; charset=utf-8";
            var json = JsonSerializer.Serialize(ApiResponse.Fail(message));
            await ctx.Response.WriteAsync(json);
        }
    }
}
