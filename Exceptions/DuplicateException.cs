using System;

namespace LicenseService.Exceptions;

public sealed class DuplicateException : Exception
{
  public DuplicateException() { }
  public DuplicateException(string Message) : base(Message) { }
  public DuplicateException(string Entity, string Data) : base($"Record duplicated {Entity}:{Data}") { }
  public DuplicateException(string Message, Exception innerException) : base(Message, innerException) { }
}
