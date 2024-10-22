using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using MolenApplicatie.Server.Models;

namespace MolenApplicatie.Server.Filters
{
    public class FileUploadFilter : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext actionContext, ActionExecutionDelegate next)
        {
            var context = actionContext.HttpContext;
            if (!context.Request.Headers.ContainsKey("Authorization"))
            {
                context.Response.StatusCode = 401;
                return;
            }

            var helloOptions =
              context.RequestServices.GetService<IOptions<FileUploadOptions>>() switch
              {
                  { Value: var __ } => __,
                  _ => new FileUploadOptions() { Authorization = Guid.NewGuid().ToString() }
              };

            if (context.Request.Headers["Authorization"] != helloOptions.Authorization)
            {
                context.Response.StatusCode = 401;
                return;
            }
            await next();
        }
    }
}
