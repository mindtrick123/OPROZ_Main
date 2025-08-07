using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OPROZ_Main.Models;

namespace OPROZ_Main.Data;

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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure ApplicationUser - Company relationship
        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Company)
            .WithMany(c => c.Users)
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Service - Company relationship
        builder.Entity<Service>()
            .HasOne(s => s.Company)
            .WithMany(c => c.Services)
            .HasForeignKey(s => s.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Offer - Service relationship
        builder.Entity<Offer>()
            .HasOne(o => o.Service)
            .WithMany(s => s.Offers)
            .HasForeignKey(o => o.ServiceId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Offer - SubscriptionPlan relationship
        builder.Entity<Offer>()
            .HasOne(o => o.SubscriptionPlan)
            .WithMany(sp => sp.Offers)
            .HasForeignKey(o => o.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure Offer - Company relationship
        builder.Entity<Offer>()
            .HasOne(o => o.Company)
            .WithMany(c => c.Offers)
            .HasForeignKey(o => o.CompanyId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure PaymentHistory - ApplicationUser relationship
        builder.Entity<PaymentHistory>()
            .HasOne(ph => ph.User)
            .WithMany()
            .HasForeignKey(ph => ph.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure PaymentHistory - Offer relationship
        builder.Entity<PaymentHistory>()
            .HasOne(ph => ph.Offer)
            .WithMany(o => o.PaymentHistories)
            .HasForeignKey(ph => ph.OfferId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure HelpQuery - ApplicationUser relationship
        builder.Entity<HelpQuery>()
            .HasOne(hq => hq.User)
            .WithMany()
            .HasForeignKey(hq => hq.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure AuditLog - ApplicationUser relationship
        builder.Entity<AuditLog>()
            .HasOne(al => al.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure decimal precision
        builder.Entity<Service>()
            .Property(s => s.Price)
            .HasPrecision(18, 2);

        builder.Entity<SubscriptionPlan>()
            .Property(sp => sp.Price)
            .HasPrecision(18, 2);

        builder.Entity<Offer>()
            .Property(o => o.Price)
            .HasPrecision(18, 2);

        builder.Entity<Offer>()
            .Property(o => o.DiscountPercentage)
            .HasPrecision(5, 2);

        builder.Entity<PaymentHistory>()
            .Property(ph => ph.Amount)
            .HasPrecision(18, 2);

        // Configure indexes for better performance
        builder.Entity<ApplicationUser>()
            .HasIndex(u => u.Email)
            .IsUnique();

        builder.Entity<ApplicationUser>()
            .HasIndex(u => u.CompanyId);

        builder.Entity<Company>()
            .HasIndex(c => c.Name);

        builder.Entity<Service>()
            .HasIndex(s => s.CompanyId);

        builder.Entity<PaymentHistory>()
            .HasIndex(ph => ph.TransactionId)
            .IsUnique();

        builder.Entity<PaymentHistory>()
            .HasIndex(ph => ph.RazorpayPaymentId);

        builder.Entity<HelpQuery>()
            .HasIndex(hq => hq.Email);

        builder.Entity<AuditLog>()
            .HasIndex(al => al.CreatedAt);

        builder.Entity<AuditLog>()
            .HasIndex(al => al.UserId);
    }
}