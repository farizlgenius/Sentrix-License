using System;
using System.Net;
using LicenseService.Exceptions;
using LicenseService.Helper;
using LicenseService.Model;


namespace Host.Middlewares;

public sealed class GlobalException : IMiddleware
{
  private readonly ILogger<GlobalException> _logger;

  public GlobalException(ILogger<GlobalException> logger)
  {
    _logger = logger;
  }
  public async Task InvokeAsync(HttpContext context, RequestDelegate next)
  {
    try
    {

      // Capture body before MVC binding
      var requestBody = await ExceptionHelper.GetRequestBodyAsync(context);

      // Store it in HttpContext.Items
      context.Items["RequestBody"] = requestBody;

      await next(context);
    }
    catch (Exception ex)
    {
      await ExceptionSwitcher(context, ex);
    }
  }

  private async Task ExceptionSwitcher(HttpContext context, Exception ex)
  {
    var requestBody = await ExceptionHelper.GetRequestBodyAsync(context);

    switch (ex)
    {
      case BadRequestException:
      case ArgumentException:
      case DefaultRecordException:
        await BadRequestExceptionHandler(context, ex);
        ExceptionHelper.LogException(_logger, context, ex, LogLevel.Warning, "Bad request", requestBody);
        break;

      case UnauthorizedAccessException:
        await UnauthorizedAccessExceptionHandler(context, ex);
        ExceptionHelper.LogException(_logger, context, ex, LogLevel.Warning, "Unauthorized access", requestBody);
        break;

      case ForbiddenException:
        await ForbiddenExceptionHandler(context, ex);
        ExceptionHelper.LogException(_logger, context, ex, LogLevel.Warning, "Forbidden access", requestBody);
        break;

      case NotFoundException:
        await NotFoundExceptionHandler(context, ex);
        ExceptionHelper.LogException(_logger, context, ex, LogLevel.Warning, "Not found", requestBody);
        break;

      case DuplicateException:
        await DuplicateExceptionHandler(context, ex);
        ExceptionHelper.LogException(_logger, context, ex, LogLevel.Warning, "Duplicated", requestBody);
        break;

      case FoundRelateException:
        await FoundRelateExceptionHandler(context, ex);
        ExceptionHelper.LogException(_logger, context, ex, LogLevel.Warning, "Found related", requestBody);
        break;

      default:
        await HandleException(context, ex);
        ExceptionHelper.LogException(_logger, context, ex, LogLevel.Error, "Internal server error", requestBody);
        break;
    }
  }

  private Task BadRequestExceptionHandler(HttpContext context, Exception ex)
  {

    // Set the response status code and content
    context.Response.StatusCode = StatusCodes.Status400BadRequest;
    context.Response.ContentType = "application/json";

    var response = new BaseResponse<object>(
          DateTime.UtcNow,
          System.Net.HttpStatusCode.BadRequest,
          false,
          "Bad request",
          Errors: new BaseErrorResponse(
                ex.Message
          ));

    return context.Response.WriteAsJsonAsync(response);


  }

  private Task FoundRelateExceptionHandler(HttpContext context, Exception ex)
  {

    // Set the response status code and content
    context.Response.StatusCode = StatusCodes.Status406NotAcceptable;
    context.Response.ContentType = "application/json";

    var response = new BaseResponse<object>(
          DateTime.UtcNow,
          System.Net.HttpStatusCode.NotAcceptable,
          false,
          "Found related",
          Errors: new BaseErrorResponse(
                ex.Message
          ));

    return context.Response.WriteAsJsonAsync(response);


  }

  private Task DuplicateExceptionHandler(HttpContext context, Exception ex)
  {

    // Set the response status code and content
    context.Response.StatusCode = StatusCodes.Status400BadRequest;
    context.Response.ContentType = "application/json";

    var response = new BaseResponse<object>(
          DateTime.UtcNow,
          System.Net.HttpStatusCode.BadRequest,
          false,
          "Duplicated",
          Errors: new BaseErrorResponse(
                ex.Message
          ));

    return context.Response.WriteAsJsonAsync(response);
  }


  private Task UnauthorizedAccessExceptionHandler(HttpContext context, Exception ex)
  {
    // Set the response status code and content
    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
    context.Response.ContentType = "application/json";

    var response = new BaseResponse<object>(
          DateTime.UtcNow,
          System.Net.HttpStatusCode.Unauthorized,
          false,
          "Unauthorized",
          Errors: new BaseErrorResponse(
                ex.Message
          ));

    return context.Response.WriteAsJsonAsync(response);
  }

  private Task NotFoundExceptionHandler(HttpContext context, Exception ex)
  {


    // Set the response status code and content
    context.Response.StatusCode = StatusCodes.Status404NotFound;
    context.Response.ContentType = "application/json";

    var response = new BaseResponse<object>(
          DateTime.UtcNow,
          System.Net.HttpStatusCode.NotFound,
          false,
          "Not found",
          Errors: new BaseErrorResponse(
                ex.Message
          ));
    return context.Response.WriteAsJsonAsync(response);
  }

  private Task ForbiddenExceptionHandler(HttpContext context, Exception ex)
  {

    // Set the response status code and content
    context.Response.StatusCode = StatusCodes.Status403Forbidden;
    context.Response.ContentType = "application/json";

    var response = new BaseResponse<object>(
          DateTime.UtcNow,
          System.Net.HttpStatusCode.Forbidden,
          false,
          "Forbidden",
          Errors: new BaseErrorResponse(
                ex.Message
          ));

    return context.Response.WriteAsJsonAsync(response);
  }

  private Task HandleException(HttpContext context, Exception ex)
  {
    // Set the response status code and content
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/json";

    if (ex.InnerException is null)
    {
      return context.Response.WriteAsJsonAsync(
             new BaseResponse<object>(
                  DateTime.UtcNow,
                  System.Net.HttpStatusCode.InternalServerError,
                  false,
                  "Internal server error",
                  Errors: new BaseErrorResponse(
                        ex.Message
                  ))
      );
    }
    else
    {
      return context.Response.WriteAsJsonAsync(
            new BaseResponse<object>(
                 DateTime.UtcNow,
                 System.Net.HttpStatusCode.InternalServerError,
                 false,
                 "Internal server error",
                 Errors: new BaseErrorResponse(
                       ex.Message,
                       ex.InnerException.ToString(),
                       ex.StackTrace
                 ))
     );
    }

  }
}
