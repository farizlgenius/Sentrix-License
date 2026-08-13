using System.ComponentModel.DataAnnotations;

namespace LicenseService.Entities;

public class BaseEntity
{
  [Key]
  public int id { get; set; }
  public Guid guid { get; set; }
  public DateTime created_at { get; set; }
  public DateTime expire_at { get; set; }

  public BaseEntity() { }

}