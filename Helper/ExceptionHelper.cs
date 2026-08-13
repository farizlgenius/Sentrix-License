namespace LicenseService.Helper;

public static class ExceptionHelper
{
  public static async Task<string> GetRequestBodyAsync(HttpContext context)
  {
    context.Request.EnableBuffering();

    context.Request.Body.Position = 0;

    using var reader = new StreamReader(
        context.Request.Body,
        leaveOpen: true);

    var body = await reader.ReadToEndAsync();

    context.Request.Body.Position = 0;

    return body;
  }

  public static void LogException(
        ILogger logger,
        HttpContext context,
        Exception ex,
        LogLevel logLevel,
        string message,
        string requestBody
        )
  {
    logger.Log(
        logLevel,
        ex,
        "{Message}. {Method} {Path} {QueryString} {RequestBody}",
        message,
        context.Request.Method,
        context.Request.Path,
        context.Request.QueryString,
        requestBody
        );
  }
}