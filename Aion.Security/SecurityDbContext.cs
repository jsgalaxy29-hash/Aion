using System;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Aion.DataEngine.Entities;

namespace Aion.Security
{
    public class SecurityDbContext : IdentityDbContext<SUser, IdentityRole<Guid>, Guid>
    {
        public SecurityDbContext(DbContextOptions<SecurityDbContext> options) : base(options) {}
        public DbSet<SGroup> SGroup => Set<SGroup>();
        public DbSet<SUserGroup> SUserGroup => Set<SUserGroup>();
        public DbSet<SRightType> SRightType => Set<SRightType>();
        public DbSet<SRight> SRight => Set<SRight>();
        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);
            b.Entity<SGroup>().ToTable("SGroupe").HasIndex(x => x.Name).IsUnique();
            b.Entity<SUserGroup>().ToTable("SUserGroup").HasIndex(x => new { x.UserId, x.GroupId }).IsUnique();
            b.Entity<SRightType>().ToTable("SRightType").HasIndex(x => x.Code).IsUnique();
            b.Entity<SRight>().ToTable("SRight").HasIndex(x => new { x.GroupId, x.Target, x.SubjectId }).IsUnique();
        }
    }
}
