using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SesliDil.Core.Responses;

namespace SesliDilDeneme.API.Filters
{
    /// <summary>
    /// ModelState hatalarını tek tip ApiResponse formatında döndürür.
    /// Not: Program.cs içinde SuppressModelStateInvalidFilter = true yapılacak.
    /// </summary>
    public class ApiResponseValidationFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        k => k.Key,
                        v => v.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                    );

                var payload = new { Errors = errors };
                var resp = ApiResponse<object>.Fail("Doğrulama hatası.", payload);

                context.Result = new BadRequestObjectResult(resp);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
