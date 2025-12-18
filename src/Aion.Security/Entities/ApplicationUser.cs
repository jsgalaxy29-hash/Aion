using System;
using Microsoft.AspNetCore.Identity;
namespace Aion.Security.Entities { public class ApplicationUser : IdentityUser<int> { public bool Actif { get; set; } = true; } }
