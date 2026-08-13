using System;
using System.ComponentModel.DataAnnotations;
using LicenseService.Enums;
using LicenseService.Model;

namespace LicenseService.Entities;

public sealed class License : BaseEntity
{
  public string company { get; set; } = string.Empty;
  public string customer_site { get; set; } = string.Empty;
  public string machine_id { get; set; } = string.Empty;
  public byte[] license { get; set; } = Array.Empty<byte>();
  public LicenseType license_type { get; set; }
  public Guid sign_key_guid { get; set; }
  public SignKeyAudit? sign_key { get; set; }

  public License() { }

  public License(
    string Company,
    string CustomerSite,
    string MachineId,
    byte[] License,
    LicenseType LicenseType,
    Guid signKeyGuid,
    DateTime Exp
    )
  {
    company = Company;
    customer_site = CustomerSite;
    machine_id = MachineId;
    license = License;
    license_type = LicenseType;
    sign_key_guid = signKeyGuid;
  }
}
