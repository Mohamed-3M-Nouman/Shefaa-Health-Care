using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShefaaHealthCare.Models;

namespace ShefaaHealthCare.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {

        public DbSet<Patient> Patients { get; set; } = null!;
        public DbSet<Doctor> Doctors { get; set; } = null!;
        public DbSet<Specialization> Specializations { get; set; } = null!;
        public DbSet<DoctorSchedule> DoctorSchedules { get; set; } = null!;
        public DbSet<Appointment> Appointments { get; set; } = null!;
        public DbSet<PatientMedicalProfile> PatientMedicalProfiles { get; set; } = null!;
        public DbSet<MedicalRecord> MedicalRecords { get; set; } = null!;
        public DbSet<MedicalAttachment> MedicalAttachments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ── 1-to-1: ApplicationUser ↔ Patient ──
            builder.Entity<Patient>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            builder.Entity<Patient>()
                .HasOne(p => p.User)
                .WithOne(u => u.Patient)
                .HasForeignKey<Patient>(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── 1-to-1: ApplicationUser ↔ Doctor ──
            builder.Entity<Doctor>()
                .HasIndex(d => d.UserId)
                .IsUnique();

            builder.Entity<Doctor>()
                .HasOne(d => d.User)
                .WithOne(u => u.Doctor)
                .HasForeignKey<Doctor>(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── 1-to-1: Patient ↔ PatientMedicalProfile ──
            builder.Entity<PatientMedicalProfile>()
                .HasIndex(pmp => pmp.PatientId)
                .IsUnique();

            builder.Entity<PatientMedicalProfile>()
                .HasOne(pmp => pmp.Patient)
                .WithOne(p => p.PatientMedicalProfile)
                .HasForeignKey<PatientMedicalProfile>(pmp => pmp.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── 1-to-Many: Doctor → DoctorSchedules ──
            builder.Entity<DoctorSchedule>()
                .HasOne(ds => ds.Doctor)
                .WithMany(d => d.Schedules)
                .HasForeignKey(ds => ds.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── 1-to-Many: Patient → Appointments ──
            builder.Entity<Appointment>()
                .HasOne(a => a.Patient)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── 1-to-Many: Doctor → Appointments ──
            builder.Entity<Appointment>()
                .HasOne(a => a.Doctor)
                .WithMany(d => d.Appointments)
                .HasForeignKey(a => a.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── 1-to-Many: PatientMedicalProfile → MedicalRecords ──
            builder.Entity<MedicalRecord>()
                .HasOne(mr => mr.PatientMedicalProfile)
                .WithMany(pmp => pmp.MedicalRecords)
                .HasForeignKey(mr => mr.PatientMedicalProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── 1-to-Many: Doctor → MedicalRecords ──
            builder.Entity<MedicalRecord>()
                .HasOne(mr => mr.Doctor)
                .WithMany(d => d.MedicalRecords)
                .HasForeignKey(mr => mr.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── 1-to-Many: PatientMedicalProfile → MedicalAttachments ──
            builder.Entity<MedicalAttachment>()
                .HasOne(ma => ma.PatientMedicalProfile)
                .WithMany(pmp => pmp.MedicalAttachments)
                .HasForeignKey(ma => ma.PatientMedicalProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Decimal Precision ──
            builder.Entity<Doctor>()
                .Property(d => d.ConsultationFee)
                .HasColumnType("decimal(18,2)");
        }
    }
}
