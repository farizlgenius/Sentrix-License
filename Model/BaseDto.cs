using System;
using System.Net;

namespace LicenseService.Model;

public record BaseDto<T>(HttpStatusCode Code, T Payload, Guid Guid, string Message, DateTime TimeStamp);
