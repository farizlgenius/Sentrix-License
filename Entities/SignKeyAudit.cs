using System;
using System.ComponentModel.DataAnnotations;
using LicenseService.Model;

namespace LicenseService.Entities;

public sealed class SignKeyAudit : BaseEntity
{
  public byte[] sign_pub { get; set; } = default!;
  public byte[] sign_priv { get; set; } = default!;
  public bool is_revoked { get; set; } = false;
  public ICollection<License>? licenses { get; set; }

  public SignKeyAudit() { }

  public SignKeyAudit(
    byte[] Pub,
    byte[] Pri,
  DateTime Exp
  )
  {
    sign_pub = Pub;
    sign_priv = Pri;
    expire_at = Exp;
  }
}
