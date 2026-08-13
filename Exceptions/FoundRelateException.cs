using System;
namespace LicenseService.Exceptions;

public class FoundRelateException : Exception
{
  public FoundRelateException() { }
  public FoundRelateException(string Message) : base(Message) { }
  public FoundRelateException(string Entity, string Data, string Relate) : base($"Found {Relate} relate with {Entity}:{Data}") { }
  public FoundRelateException(string Message, Exception innerException) : base(Message, innerException) { }
}
