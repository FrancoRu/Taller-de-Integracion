using Club12.DTOs.Abstract;
using Club12.Viewmodels.Abstract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

public class ResponseWrapperFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // No action required on executing
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Result is ObjectResult objectResult)
        {
            int statusCode = objectResult.StatusCode ?? (context.Exception == null ? (int)HttpStatusCode.OK : (int)HttpStatusCode.InternalServerError);
            string? errorMessage = null;
            object? content = null;

            if (statusCode is >= 200 and < 300)
            {
                content = objectResult.Value;
            }
            else
            {
                errorMessage = objectResult.Value?.ToString();
            }

            Type contentType = content?.GetType() ?? typeof(GenericEntity);
            Type responseType = typeof(BaseResponse<>).MakeGenericType(contentType);

            object? response = Activator.CreateInstance(responseType, statusCode, content, errorMessage);

            context.Result = new ObjectResult(response) { StatusCode = statusCode };

            context.Exception = null;
        }
    }

}

