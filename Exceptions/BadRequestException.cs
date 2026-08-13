using System;

namespace LicenseService.Exceptions;

public sealed class BadRequestException : Exception
{
  public BadRequestException() { }
  public BadRequestException(string Entity, string Data) : base($"Bad requested on {Entity}:{Data}") { }
  public BadRequestException(string Message) : base(Message) { }
  public BadRequestException(string Message, Exception innerException) : base(Message, innerException) { }
}
