using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OPROZ_Main.Models;

namespace OPROZ_Main.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Company> Companies { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<Offer> Offers { get; set; }
        public DbSet<PaymentHistory> PaymentHistories { get; set; }
        public DbSet<HelpQuery> HelpQueries { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public new DbSet<Role> Roles { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ApplicationUser
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.HasOne(d => d.Company)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.CompanyId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(d => d.Role)
                    .WithMany(p => p.Users)
                    .HasForeignKey(d => d.RoleId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Role
            builder.Entity<Role>(entity =>
            {
                entity.HasIndex(e => e.Name)
                    .IsUnique();
            });

            // Configure PaymentHistory
            builder.Entity<PaymentHistory>(entity =>
            {
                entity.HasOne(d => d.User)
                    .WithMany(p => p.PaymentHistories)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Company)
                    .WithMany(p => p.PaymentHistories)
                    .HasForeignKey(d => d.CompanyId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(d => d.SubscriptionPlan)
                    .WithMany(p => p.PaymentHistories)
                    .HasForeignKey(d => d.SubscriptionPlanId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Offer)
                    .WithMany(p => p.PaymentHistories)
                    .HasForeignKey(d => d.OfferId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(e => e.Amount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.DiscountAmount)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.FinalAmount)
                    .HasColumnType("decimal(18,2)");
            });

            // Configure SubscriptionPlan
            builder.Entity<SubscriptionPlan>(entity =>
            {
                entity.HasOne(d => d.Service)
                    .WithMany(p => p.SubscriptionPlans)
                    .HasForeignKey(d => d.ServiceId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.Price)
                    .HasColumnType("decimal(18,2)");
            });

            // Configure Service
            builder.Entity<Service>(entity =>
            {
                entity.Property(e => e.BasePrice)
                    .HasColumnType("decimal(18,2)");
            });

            // Configure Offer
            builder.Entity<Offer>(entity =>
            {
                entity.HasOne(d => d.Service)
                    .WithMany(p => p.Offers)
                    .HasForeignKey(d => d.ServiceId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.Property(e => e.Value)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.MinOrderAmount)
                    .HasColumnType("decimal(18,2)");

                entity.HasIndex(e => e.Code)
                    .IsUnique();
            });

            // Configure HelpQuery
            builder.Entity<HelpQuery>(entity =>
            {
                entity.HasOne(d => d.User)
                    .WithMany(p => p.HelpQueries)
                    .HasForeignKey(d => d.UserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure indexes for better performance
            builder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.TableName);
            });

            builder.Entity<PaymentHistory>(entity =>
            {
                entity.HasIndex(e => e.TransactionId)
                    .IsUnique();
                entity.HasIndex(e => e.RazorpayPaymentId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.PaymentDate);
            });

            builder.Entity<HelpQuery>(entity =>
            {
                entity.HasIndex(e => e.Email);
                entity.HasIndex(e => e.Status);
                entity.HasIndex(e => e.CreatedAt);
            });
        }
    }
}