using HomeownersSubdivision.Models;
using Microsoft.EntityFrameworkCore;
using HomeownersSubdivision.Services;
using HomeownersSubdivision.Models.Analytics;

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
        public DbSet<ServiceRequest> ServiceRequests { get; set; } = null!;
        public DbSet<ServiceRequestUpdate> ServiceRequestUpdates { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<Bill> Bills { get; set; } = null!;
        public DbSet<Payment> Payments { get; set; } = null!;
        public DbSet<PaymentMethod> PaymentMethods { get; set; } = null!;
        public DbSet<RefundRequest> RefundRequests { get; set; } = null!;
        public DbSet<Facility> Facilities { get; set; } = null!;
        public DbSet<FacilityReservation> FacilityReservations { get; set; } = null!;
        public DbSet<ContactDirectory> ContactDirectory { get; set; } = null!;
        
        // Forum-related DbSets
        public DbSet<ForumCategory> ForumCategories { get; set; } = null!;
        public DbSet<ForumTopic> ForumTopics { get; set; } = null!;
        public DbSet<ForumPost> ForumPosts { get; set; } = null!;
        public DbSet<ForumReaction> ForumReactions { get; set; } = null!;

        // Security-related DbSets
        public DbSet<VisitorPass> VisitorPasses { get; set; } = null!;
        public DbSet<VehicleRegistration> VehicleRegistrations { get; set; } = null!;
        public DbSet<EmergencyContact> EmergencyContacts { get; set; } = null!;
        
        // Feedback and Complaints
        public DbSet<Feedback> Feedbacks { get; set; } = null!;
        
        // Analytics and Reports
        public DbSet<ReportDefinition> ReportDefinitions { get; set; } = null!;
        public DbSet<ReportParameter> ReportParameters { get; set; } = null!;
        public DbSet<SavedReport> SavedReports { get; set; } = null!;
        public DbSet<DashboardWidget> DashboardWidgets { get; set; } = null!;

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
            modelBuilder.Entity<ServiceRequest>()
                .HasOne(m => m.Homeowner)
                .WithMany()
                .HasForeignKey(m => m.HomeownerId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure the relationship between MaintenanceRequest and User (AssignedTo)
            modelBuilder.Entity<ServiceRequest>()
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
                
            // Configure relationships for ContactDirectory
            modelBuilder.Entity<ContactDirectory>()
                .HasOne(cd => cd.CreatedBy)
                .WithMany()
                .HasForeignKey(cd => cd.CreatedById)
                .OnDelete(DeleteBehavior.SetNull);
                
            modelBuilder.Entity<ContactDirectory>()
                .HasOne(cd => cd.UpdatedBy)
                .WithMany()
                .HasForeignKey(cd => cd.UpdatedById)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Create indexes for ContactDirectory
            modelBuilder.Entity<ContactDirectory>()
                .HasIndex(cd => cd.Category);
                
            modelBuilder.Entity<ContactDirectory>()
                .HasIndex(cd => cd.SortOrder);
                
            // Configure relationships for Forum models
            modelBuilder.Entity<ForumTopic>()
                .HasOne(t => t.Category)
                .WithMany(c => c.Topics)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<ForumTopic>()
                .HasOne(t => t.CreatedBy)
                .WithMany(u => u.ForumTopics)
                .HasForeignKey(t => t.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<ForumPost>()
                .HasOne(p => p.Topic)
                .WithMany(t => t.Posts)
                .HasForeignKey(p => p.TopicId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<ForumPost>()
                .HasOne(p => p.CreatedBy)
                .WithMany(u => u.ForumPosts)
                .HasForeignKey(p => p.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<ForumPost>()
                .HasOne(p => p.ParentPost)
                .WithMany(p => p.Replies)
                .HasForeignKey(p => p.ParentPostId)
                .OnDelete(DeleteBehavior.SetNull);
                
            modelBuilder.Entity<ForumReaction>()
                .HasOne(r => r.Post)
                .WithMany(p => p.Reactions)
                .HasForeignKey(r => r.PostId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<ForumReaction>()
                .HasOne(r => r.User)
                .WithMany(u => u.ForumReactions)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Create indexes for Forum models for faster queries
            modelBuilder.Entity<ForumTopic>()
                .HasIndex(t => t.IsPinned);
                
            modelBuilder.Entity<ForumTopic>()
                .HasIndex(t => t.LastActivityAt);
                
            modelBuilder.Entity<ForumPost>()
                .HasIndex(p => p.CreatedAt);
                
            modelBuilder.Entity<ForumReaction>()
                .HasIndex(r => new { r.PostId, r.UserId, r.Type })
                .IsUnique();

            // Configure relationships for VisitorPass
            modelBuilder.Entity<VisitorPass>()
                .HasOne(v => v.RequestedBy)
                .WithMany()
                .HasForeignKey(v => v.RequestedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VisitorPass>()
                .HasOne(v => v.ApprovedBy)
                .WithMany()
                .HasForeignKey(v => v.ApprovedById)
                .OnDelete(DeleteBehavior.SetNull);

            // Create indexes for VisitorPass
            modelBuilder.Entity<VisitorPass>()
                .HasIndex(v => v.Status);

            modelBuilder.Entity<VisitorPass>()
                .HasIndex(v => v.VisitDate);

            // Configure relationships for VehicleRegistration
            modelBuilder.Entity<VehicleRegistration>()
                .HasOne(v => v.Owner)
                .WithMany()
                .HasForeignKey(v => v.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Create indexes for VehicleRegistration
            modelBuilder.Entity<VehicleRegistration>()
                .HasIndex(v => v.LicensePlate)
                .IsUnique();

            modelBuilder.Entity<VehicleRegistration>()
                .HasIndex(v => v.IsActive);

            // Configure relationships for EmergencyContact
            modelBuilder.Entity<EmergencyContact>()
                .HasOne(e => e.Homeowner)
                .WithMany()
                .HasForeignKey(e => e.HomeownerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Create indexes for EmergencyContact
            modelBuilder.Entity<EmergencyContact>()
                .HasIndex(e => e.IsActive);

            modelBuilder.Entity<EmergencyContact>()
                .HasIndex(e => e.Type);
                
            // Configure relationships for Feedback
            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.SubmittedBy)
                .WithMany()
                .HasForeignKey(f => f.SubmittedById)
                .OnDelete(DeleteBehavior.Restrict);
                
            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.ProcessedBy)
                .WithMany()
                .HasForeignKey(f => f.ProcessedById)
                .OnDelete(DeleteBehavior.SetNull);
                
            modelBuilder.Entity<Feedback>()
                .HasOne(f => f.Homeowner)
                .WithMany()
                .HasForeignKey(f => f.HomeownerId)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Create indexes for Feedback
            modelBuilder.Entity<Feedback>()
                .HasIndex(f => f.Status);
                
            modelBuilder.Entity<Feedback>()
                .HasIndex(f => f.Type);
                
            modelBuilder.Entity<Feedback>()
                .HasIndex(f => f.CreatedAt);

            // Configure relationships for ReportDefinition
            modelBuilder.Entity<ReportDefinition>()
                .HasOne(r => r.CreatedBy)
                .WithMany()
                .HasForeignKey(r => r.CreatedById)
                .OnDelete(DeleteBehavior.SetNull);
                
            modelBuilder.Entity<ReportDefinition>()
                .HasOne(r => r.LastModifiedBy)
                .WithMany()
                .HasForeignKey(r => r.LastModifiedById)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Configure relationships for ReportParameter
            modelBuilder.Entity<ReportParameter>()
                .HasOne(p => p.ReportDefinition)
                .WithMany()
                .HasForeignKey(p => p.ReportDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Configure relationships for SavedReport
            modelBuilder.Entity<SavedReport>()
                .HasOne(s => s.ReportDefinition)
                .WithMany()
                .HasForeignKey(s => s.ReportDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<SavedReport>()
                .HasOne(s => s.GeneratedBy)
                .WithMany()
                .HasForeignKey(s => s.GeneratedById)
                .OnDelete(DeleteBehavior.SetNull);
                
            // Configure relationships for DashboardWidget
            modelBuilder.Entity<DashboardWidget>()
                .HasOne(w => w.ReportDefinition)
                .WithMany()
                .HasForeignKey(w => w.ReportDefinitionId)
                .OnDelete(DeleteBehavior.SetNull);
                
            modelBuilder.Entity<DashboardWidget>()
                .HasOne(w => w.CreatedBy)
                .WithMany()
                .HasForeignKey(w => w.CreatedById)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
} 