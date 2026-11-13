using ExpenseReimbursement.Api.Common;
using ExpenseReimbursement.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ExpenseReimbursement.Api.Infrastructure;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ExpenseRequest> ExpenseRequests => Set<ExpenseRequest>();
    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.Property(e => e.Name).IsRequired().HasMaxLength(Employee.NameMaxLength);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.AccessCode).IsRequired().HasMaxLength(AccessCodeGenerator.Length);
            entity.HasIndex(e => e.AccessCode).IsUnique();
        });

        modelBuilder.Entity<ExpenseRequest>(entity =>
        {
            entity.Property(e => e.Category).IsRequired().HasMaxLength(ExpenseRequest.CategoryMaxLength);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(ExpenseRequest.DescriptionMaxLength);
            entity.Property(e => e.RejectionReason).HasMaxLength(ExpenseRequest.RejectionReasonMaxLength);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Status).HasConversion<string>();
            entity.HasOne(e => e.Employee)
                .WithMany()
                .HasForeignKey(e => e.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
