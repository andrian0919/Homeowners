using HomeownersSubdivision.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeownersSubdivision.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Homeowner> Homeowners { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Event> Events { get; set; } = null!;
        public DbSet<Announcement> Announcements { get; set; } = null!;
        public DbSet<MaintenanceRequest> MaintenanceRequests { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<Bill> Bills { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<PaymentMethod> PaymentMethods { get; set; } = null!;
        public DbSet<RefundRequest> RefundRequests { get; set; } = null!;
        public DbSet<Facility> Facilities { get; set; } = null!;
        public DbSet<FacilityReservation> FacilityReservations { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure the relationship between User and Homeowner
            modelBuilder.Entity<User>()
                .HasOne(u => u.Homeowner)
                .WithMany()
                .HasForeignKey(u => u.HomeownerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Create an index on email to ensure uniqueness
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Create an index on username to ensure uniqueness
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
                
            // Configure the relationship between Event and User (Creator)
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure the relationship between Announcement and User (Creator)
            modelBuilder.Entity<Announcement>()
                .HasOne(a => a.Creator)
                .WithMany()
                .HasForeignKey(a => a.CreatedBy)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure the relationship between MaintenanceRequest and Homeowner
            modelBuilder.Entity<MaintenanceRequest>()
                .HasOne(m => m.Homeowner)
                .WithMany()
                .HasForeignKey(m => m.HomeownerId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure the relationship between MaintenanceRequest and User (AssignedTo)
            modelBuilder.Entity<MaintenanceRequest>()
                .HasOne(m => m.AssignedTo)
                .WithMany()
                .HasForeignKey(m => m.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Configure the relationship between Notification and User
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Create an index on notification status and user for faster queries
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.Status });
                
            // Configure the relationship between Bill and User
            modelBuilder.Entity<Bill>()
                .HasOne(b => b.User)
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure the relationship between Payment and Bill
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Bill)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BillId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Configure the relationship between Payment and User
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure the relationship between Payment and PaymentMethod
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.PaymentMethod)
                .WithMany()
                .HasForeignKey(p => p.PaymentMethodId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Configure the relationship between PaymentMethod and User
            modelBuilder.Entity<PaymentMethod>()
                .HasOne(pm => pm.User)
                .WithMany()
                .HasForeignKey(pm => pm.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Create index on bill status for faster queries
            modelBuilder.Entity<Bill>()
                .HasIndex(b => b.Status);
                
            // Create composite index on user and due date for faster queries
            modelBuilder.Entity<Bill>()
                .HasIndex(b => new { b.UserId, b.DueDate });

            // Configure relationships for RefundRequest
            modelBuilder.Entity<RefundRequest>()
                .HasOne(r => r.Payment)
                .WithMany(p => p.RefundRequests)
                .HasForeignKey(r => r.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RefundRequest>()
                .HasOne(r => r.User)
                .WithMany(u => u.RefundRequests)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure indexes for RefundRequest
            modelBuilder.Entity<RefundRequest>()
                .HasIndex(r => r.Status);

            modelBuilder.Entity<RefundRequest>()
                .HasIndex(r => r.RequestDate);
                
            // Configure relationships for Facility and FacilityReservation
            modelBuilder.Entity<FacilityReservation>()
                .HasOne(fr => fr.Facility)
                .WithMany(f => f.Reservations)
                .HasForeignKey(fr => fr.FacilityId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<FacilityReservation>()
                .HasOne(fr => fr.User)
                .WithMany(u => u.FacilityReservations)
                .HasForeignKey(fr => fr.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Create indexes for FacilityReservation
            modelBuilder.Entity<FacilityReservation>()
                .HasIndex(fr => fr.Status);
                
            modelBuilder.Entity<FacilityReservation>()
                .HasIndex(fr => fr.ReservationDate);
                
            modelBuilder.Entity<FacilityReservation>()
                .HasIndex(fr => new { fr.FacilityId, fr.ReservationDate });
        }
    }
} 