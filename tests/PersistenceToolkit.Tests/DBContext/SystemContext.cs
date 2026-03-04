namespace PersistenceToolkit.Tests.DBContext
{
    using Microsoft.EntityFrameworkCore;
    using PersistenceToolkit.Tests.Entities;
    using PersistenceToolkit.Persistence.Persistence;

    public class SystemContext : BaseContext
    {
        private readonly string connectionString;
        public SystemContext(string connectionString) : base(connectionString)
        {

        }
        public SystemContext(DbContextOptions<BaseContext> options) : base(options)
        {
        }

        //public virtual DbSet<ParentTable> ParentTables { get; set; }
        //public virtual DbSet<ChildTable> ChildTables { get; set; }
        public virtual DbSet<User> Users { get; set; }

        protected override void ApplyConfiguration(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new ParentConfiguration());
            modelBuilder.ApplyConfiguration(new ChildConfiguration());
            modelBuilder.ApplyConfiguration(new GrandChildConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
        }
        protected override void DefineManualConfiguration(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<ChildTable>()
            //            .ToTable("ChildTable")
            //            .HasKey(e => e.Id);

            //modelBuilder.Entity<User>()
            //            .ToTable("User")
            //            .HasKey(e => e.Id);
        }
    }
}
