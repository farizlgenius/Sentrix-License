using System;

namespace LicenseService.Exceptions;

public sealed class UnauthorizedException : Exception
{
  public UnauthorizedException() { }
  public UnauthorizedException(string Message) : base(Message) { }
  public UnauthorizedException(string Message, Exception innerException) : base(Message, innerException) { }
}
