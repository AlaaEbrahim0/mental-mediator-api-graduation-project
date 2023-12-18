using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Domain.Entites;

[Owned]
public class RefreshToken
{
    public string? Token { get; set; }
    public DateTime ExpiresOn { get; set; }
    public bool IsExpried => DateTime.UtcNow >= ExpiresOn;
    public DateTime CreatedOn { get; set; }
    public DateTime? RevokedOn{ get; set; }
    public bool IsActive => RevokedOn is null && !IsExpried;
}
