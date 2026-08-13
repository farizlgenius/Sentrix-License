using System;
namespace LicenseService.Exceptions;

public class NotFoundException : Exception
{
  public NotFoundException() { }
  public NotFoundException(string Message) : base(Message) { }
  public NotFoundException(string Entity, string Data) : base($"Record not found for {Entity}:{Data}") { }
  public NotFoundException(string Message, Exception innerException) : base(Message, innerException) { }
}
