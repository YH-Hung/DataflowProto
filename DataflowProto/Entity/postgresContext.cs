using Microsoft.EntityFrameworkCore;

namespace DataflowProto.Entity
{
    public partial class postgresContext : DbContext
    {
        public postgresContext()
        {
        }

        public postgresContext(DbContextOptions<postgresContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Girl> Girls { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql("Server=localhost;Database=postgres;User Id=dotnetApp;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Girl>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.ToTable("Girls", "dataflow");

                entity.HasIndex(e => e.Id, "girls_pk")
                    .IsUnique();

                entity.Property(e => e.MyComment).HasMaxLength(100);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
