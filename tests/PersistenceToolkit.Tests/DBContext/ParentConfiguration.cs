using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersistenceToolkit.Tests.Entities;
using PersistenceToolkit.Persistence.Configuration;
using PersistenceToolkit.Persistence.Persistence;

namespace PersistenceToolkit.Tests.DBContext
{
    public class ParentConfiguration : BaseConfiguration<Parent>
    {
        public override void Configure(EntityTypeBuilder<Parent> builder)
        {
            base.Configure(builder);
            builder.ToTable("ParentTable");

            builder.HasMany(p => p.Children).WithOne().HasForeignKey(c => c.ParentId).IsRequired(false);
            builder.HasOne(p => p.MainChild).WithOne().HasForeignKey<OneToOneChild>(p => p.ParentId).IsRequired(false);
            builder.HasMany(p => p.IgnoredChildren).WithOne().HasForeignKey(c => c.ParentId).IsRequired(false);
            builder.HasOne(p => p.IgnoredChild).WithOne().HasForeignKey<OneToOneChild>(p => p.ParentId).IsRequired(false);
            builder.HasOne(p => p.User).WithMany().HasForeignKey(p => p.CreatedBy).IsRequired();

            builder.IgnoreOnUpdate(pb => pb.User);
            builder.IgnoreOnUpdate(pb => pb.IgnoredChild);
            builder.IgnoreOnUpdate(pb => pb.IgnoredChildren);
        }
    }

    public class ChildConfiguration : BaseConfiguration<Child>
    {
        public override void Configure(EntityTypeBuilder<Child> builder)
        {
            base.Configure(builder);
            builder.ToTable("ChildTable");

            builder.HasMany(c => c.GrandChildren).WithOne().HasForeignKey(gc => gc.ChildId).IsRequired(false);
            builder.HasOne(p => p.MainGrandChild).WithOne().HasForeignKey<OneToOneGrandChild>(p => p.ChildId).IsRequired(false);
            builder.HasMany(c => c.IgnoredGrandChildren).WithOne().HasForeignKey(gc => gc.ChildId).IsRequired(false);
            builder.HasOne(p => p.IgnoredGrandChild).WithOne().HasForeignKey<OneToOneGrandChild>(p => p.ChildId).IsRequired(false);

            builder.IgnoreOnUpdate(c => c.IgnoredGrandChild);
            builder.IgnoreOnUpdate(c => c.IgnoredGrandChildren);
        }
    }
    public class OneToOneChildConfiguration : BaseConfiguration<OneToOneChild>
    {
        public override void Configure(EntityTypeBuilder<OneToOneChild> builder)
        {
            base.Configure(builder);
            builder.ToTable("OneToOneChild");
            builder.HasKey(c => c.ParentId);

            builder.HasMany(c => c.GrandChildren).WithOne().HasForeignKey(gc => gc.ChildId).IsRequired(false);
            builder.HasOne(p => p.MainGrandChild).WithOne().HasForeignKey<OneToOneGrandChild>(p => p.ChildId).IsRequired(false);
            builder.HasMany(c => c.IgnoredGrandChildren).WithOne().HasForeignKey(gc => gc.ChildId).IsRequired(false);
            builder.HasOne(p => p.IgnoredGrandChild).WithOne().HasForeignKey<OneToOneGrandChild>(p => p.ChildId).IsRequired(false);

            builder.IgnoreOnUpdate(c => c.IgnoredGrandChild);
            builder.IgnoreOnUpdate(c => c.IgnoredGrandChildren);
        }
    }
    public class GrandChildConfiguration : BaseConfiguration<GrandChild>
    {
        public override void Configure(EntityTypeBuilder<GrandChild> builder)
        {
            base.Configure(builder);
            builder.ToTable("GrandChildTable");
        }
    }
    public class OneToOneGrandChildConfiguration : BaseConfiguration<OneToOneGrandChild>
    {
        public override void Configure(EntityTypeBuilder<OneToOneGrandChild> builder)
        {
            base.Configure(builder);
            builder.ToTable("OneToOneGrandChild");
            builder.HasKey(c => c.ChildId);
        }
    }
    public class UserConfiguration : BaseConfiguration<User>
    {
        public override void Configure(EntityTypeBuilder<User> builder)
        {
            base.Configure(builder);
            builder.ToTable("User");
        }
    }
} 