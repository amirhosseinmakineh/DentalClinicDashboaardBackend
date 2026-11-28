using DentalDashboard.Domain.Accounting.Reminders;
using DentalDashboard.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DentalDashboard.Infrastracture.Accounting.Reminders;

public sealed class AccountingReminderConfiguration : IEntityTypeConfiguration<AccountingReminder>
{
    public void Configure(EntityTypeBuilder<AccountingReminder> builder)
    {
        builder.ToTable("AccountingReminders");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.UserId, x.Kind, x.TargetId }).IsUnique();
        builder.HasIndex(x => new { x.Enabled, x.Id });
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AccountingReminderDeliveryConfiguration : IEntityTypeConfiguration<AccountingReminderDelivery>
{
    public void Configure(EntityTypeBuilder<AccountingReminderDelivery> builder)
    {
        builder.ToTable("AccountingReminderDeliveries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DueDate).HasColumnType("date");
        builder.Property(x => x.ReminderDate).HasColumnType("date");
        builder.HasIndex(x => new { x.ReminderId, x.DueDate, x.ReminderDate }).IsUnique();
        builder.HasIndex(x => x.LeaseId);
        builder.HasOne<AccountingReminder>().WithMany().HasForeignKey(x => x.ReminderId).OnDelete(DeleteBehavior.Cascade);
    }
}
