using System;
using LicenseService.Model;

namespace LicenseService.Service.Impl;

public interface ILicenseService
{
  Task<EncryptedLicense> CreateLicenseDemoAsync(GenerateDemo fingerPrint);
  Task<EncryptedLicense> CreateLicenseAsync(LicensePayload payload);
  Task<ExchangeResponse> ExchangeAsync(ExchangeRequest request);
}
