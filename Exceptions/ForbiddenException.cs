using System;

namespace LicenseService.Exceptions;

public sealed class ForbiddenException : Exception
{
  public ForbiddenException() { }
  public ForbiddenException(string Message) : base(Message) { }
  public ForbiddenException(string Message, Exception innerException) : base(Message, innerException) { }
}
