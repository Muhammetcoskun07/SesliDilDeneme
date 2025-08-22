using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SesliDil.Core.Responses;
using SesliDilDeneme.API.Infrastructure; // SkipResponseWrappingAttribute için

namespace SesliDilDeneme.API.Filters
{
    public class ResponseWrappingFilter : IAsyncResultFilter
    {
        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var hasSkip = context.ActionDescriptor.EndpointMetadata
                .OfType<SkipResponseWrappingAttribute>()
                .Any();
            if (hasSkip) { await next(); return; }

            if (context.Result is ObjectResult obj)
            {
                if (obj.Value is ApiResponse ||
                    (obj.Value?.GetType().IsGenericType == true &&
                     obj.Value.GetType().GetGenericTypeDefinition() == typeof(ApiResponse<>)))
                {
                    await next();
                    return;
                }

                var status = obj.StatusCode ?? StatusCodes.Status200OK;

                if (status >= 200 && status < 300)
                {
                    var valueType = obj.Value?.GetType() ?? typeof(object);
                    var wrappedType = typeof(ApiResponse<>).MakeGenericType(valueType);
                    var okMethod = wrappedType.GetMethod("Ok", new[] { valueType, typeof(string) });
                    var apiResp = okMethod!.Invoke(null, new object?[] { obj.Value, "İşlem başarılı." });
                    context.Result = new ObjectResult(apiResp) { StatusCode = status };
                }
                else
                {
                    context.Result = new ObjectResult(ApiResponse.Fail("İşlem başarısız."))
                    { StatusCode = status };
                }
            }
            else if (context.Result is EmptyResult)
            {
                context.Result = new ObjectResult(ApiResponse.Ok())
                { StatusCode = StatusCodes.Status200OK };
            }

            await next();
        }
    }
}
