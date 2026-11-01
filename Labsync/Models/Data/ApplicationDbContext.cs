using LabSync.Models;
using Microsoft.EntityFrameworkCore;

namespace Labsync.Models.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // =========================================================
        // USERS & ROLES
        // =========================================================

        public DbSet<User> Users { get; set; }

        public DbSet<UserDetails> UserDetails { get; set; }

        public DbSet<Role> Roles { get; set; }


        // =========================================================
        // REPORTS
        // =========================================================

        public DbSet<Report> Reports { get; set; }

        public DbSet<ReportApprovalStatus>
            ReportApprovalStatuses
        { get; set; }


        // =========================================================
        // APPOINTMENTS & PATIENTS
        // =========================================================

        public DbSet<Appointment> Appointments { get; set; }

        public DbSet<Test> Tests { get; set; }

        public DbSet<Patient> Patients { get; set; }

        public DbSet<ReferralDoctor> ReferralDoctors { get; set; }

        public DbSet<Branch> Branches { get; set; }

        public DbSet<AppointmentStatus>
            AppointmentStatuses
        { get; set; }


        // =========================================================
        // PAYMENTS
        // =========================================================

        public DbSet<Payment> Payments { get; set; }

        public DbSet<PaymentMethod> PaymentMethods { get; set; }

        public DbSet<PaymentStatus> PaymentStatuses { get; set; }


        // =========================================================
        // INVOICES
        // =========================================================

        public DbSet<Invoice> Invoices { get; set; }

        public DbSet<InvoiceItem> InvoiceItems { get; set; }


        // =========================================================
        // AUDIT LOGS
        // =========================================================

        public DbSet<AuditLog> AuditLogs { get; set; }


        // =========================================================
        // MODEL CONFIGURATION
        // =========================================================

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            // =====================================================
            // PATIENT TABLE - TRIGGER FIX
            // =====================================================

            modelBuilder.Entity<Patient>()
                .ToTable(
                    "Patients",
                    tb => tb.UseSqlOutputClause(false)
                );


            // =====================================================
            // PAYMENT TABLE - TRIGGER FIX
            // =====================================================

            modelBuilder.Entity<Payment>()
                .ToTable(
                    "Payments",
                    tb => tb.UseSqlOutputClause(false)
                );


            // =====================================================
            // ROLE
            // =====================================================

            modelBuilder.Entity<Role>()
                .HasKey(r => r.Id);


            // =====================================================
            // USER → ROLE
            // =====================================================

            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // USER → USER DETAILS
            // =====================================================

            modelBuilder.Entity<UserDetails>()
                .HasOne(ud => ud.User)
                .WithOne(u => u.UserDetails)
                .HasForeignKey<UserDetails>(ud => ud.UserId)
                .OnDelete(DeleteBehavior.Cascade);


            // =====================================================
            // USER DETAILS → CURRENT SALARY
            // =====================================================

            modelBuilder.Entity<UserDetails>()
                .Property(ud => ud.CurrentSalary)
                .HasPrecision(18, 2);


            // =====================================================
            // TEST → PRICE
            // =====================================================

            modelBuilder.Entity<Test>()
                .Property(t => t.Price)
                .HasPrecision(18, 2);


            // =====================================================
            // REPORT → APPOINTMENT
            // =====================================================

            modelBuilder.Entity<Report>()
                .HasOne(r => r.Appointment)
                .WithMany()
                .HasForeignKey(r => r.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // REPORT → GENERATED BY USER
            // =====================================================

            modelBuilder.Entity<Report>()
                .HasOne(r => r.GeneratedByUser)
                .WithMany()
                .HasForeignKey(r => r.GeneratedBy)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // REPORT → APPROVED BY USER
            // =====================================================

            modelBuilder.Entity<Report>()
                .HasOne(r => r.ApprovedByUser)
                .WithMany()
                .HasForeignKey(r => r.ApprovedBy)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // REPORT → APPROVAL STATUS
            // =====================================================

            modelBuilder.Entity<Report>()
                .HasOne(r => r.ApprovalStatus)
                .WithMany()
                .HasForeignKey(r => r.ApprovalStatusId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // INVOICE → PATIENT
            // =====================================================

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Patient)
                .WithMany()
                .HasForeignKey(i => i.PatientId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // INVOICE → APPOINTMENT
            // =====================================================

            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Appointment)
                .WithMany()
                .HasForeignKey(i => i.AppointmentId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // INVOICE → INVOICE ITEMS
            // =====================================================

            modelBuilder.Entity<InvoiceItem>()
                .HasOne(ii => ii.Invoice)
                .WithMany(i => i.InvoiceItems)
                .HasForeignKey(ii => ii.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);


            // =====================================================
            // INVOICE ITEM → TEST
            // =====================================================

            modelBuilder.Entity<InvoiceItem>()
                .HasOne(ii => ii.Test)
                .WithMany()
                .HasForeignKey(ii => ii.TestId)
                .OnDelete(DeleteBehavior.Restrict);


            // =====================================================
            // INVOICE ITEM TOTAL PRICE
            // =====================================================

            modelBuilder.Entity<InvoiceItem>()
                .Property(ii => ii.TotalPrice)
                .ValueGeneratedOnAddOrUpdate();


            // =====================================================
            // DECIMAL / MONEY PRECISION
            // =====================================================

            modelBuilder.Entity<Invoice>()
                .Property(i => i.Subtotal)
                .HasColumnType("money");

            modelBuilder.Entity<Invoice>()
                .Property(i => i.Discount)
                .HasColumnType("money");

            modelBuilder.Entity<Invoice>()
                .Property(i => i.Tax)
                .HasColumnType("money");

            modelBuilder.Entity<Invoice>()
                .Property(i => i.NetAmount)
                .HasColumnType("money");

            modelBuilder.Entity<Invoice>()
                .Property(i => i.PaidAmount)
                .HasColumnType("money");

            modelBuilder.Entity<Invoice>()
                .Property(i => i.DueAmount)
                .HasColumnType("money");


            // =====================================================
            // INVOICE ITEM MONEY
            // =====================================================

            modelBuilder.Entity<InvoiceItem>()
                .Property(ii => ii.UnitPrice)
                .HasColumnType("money");

            modelBuilder.Entity<InvoiceItem>()
                .Property(ii => ii.TotalPrice)
                .HasColumnType("money");


            // =====================================================
            // AUDIT LOG → USER
            // =====================================================

            modelBuilder.Entity<AuditLog>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}