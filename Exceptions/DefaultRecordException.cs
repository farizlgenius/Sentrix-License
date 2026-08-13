using System;

namespace LicenseService.Exceptions;

public sealed class DefaultRecordException : Exception
{
  public DefaultRecordException() { }
  public DefaultRecordException(string Message) : base(Message) { }
  public DefaultRecordException(string Method, string Entity, string Data) : base($"Default record not support for {Method}:{Entity}:{Data}") { }
  public DefaultRecordException(string Message, Exception innerException) : base(Message, innerException) { }
}
