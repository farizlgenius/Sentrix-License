using System.Net;
using LicenseService.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LicenseService.Middlewares;

public class ApiResponseFilter : IResultFilter
{
  public void OnResultExecuting(ResultExecutingContext context)
  {
    if (context.Result is not ObjectResult result)
      return;

    // Already wrapped
    if (result.Value is BaseResponse)
      return;

    var status = result.StatusCode ?? StatusCodes.Status200OK;

    context.Result = new ObjectResult(
        new BaseResponse<object>(
            DateTime.UtcNow,
            (HttpStatusCode)status,
            true,
            "Success",
            result.Value
        ))
    {
      StatusCode = status
    };
  }

  public void OnResultExecuted(ResultExecutedContext context)
  {
  }
}