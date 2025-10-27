using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Wellness_Wardens_Project.Models.AdministrationSubsystem;
using Wellness_Wardens_Project.Models.ConsumablesSubsystem;
using Wellness_Wardens_Project.Models.DoctorPatientSubsystem;
using Wellness_Wardens_Project.Models.PatientCareSubsystem;
using Wellness_Wardens_Project.Models.PatientManagementSubsystem;
namespace Wellness_Wardens_Project.Data
{
    public class ApplicationDbContext : IdentityDbContext<Employee>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options) { }

        // Admin subsystem
        public DbSet<Allergy> Allergies { get; set; }
        public DbSet<Bed> Beds { get; set; }
        public DbSet<Consumable> Consumables { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<MedicalCondition> MedicalConditions { get; set; }
        public DbSet<Medication> Medications { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Ward> Wards { get; set; }
        public DbSet<PatientMedication> PatientMedications { get; set; }

        // NEW: Many-to-Many Junction Tables
        public DbSet<PatientAllergy> PatientAllergies { get; set; }
        public DbSet<PatientMedicalCondition> PatientMedicalConditions { get; set; }

        // Consumables & Prescription subsystem
        public DbSet<ConsumablesRequest> ConsumablesRequests { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }

        // Doctor-Patient subsystem
        public DbSet<ScheduleVisit> ScheduleVisits { get; set; }

        // Patient Care subsystem
        public DbSet<DoctorVisit> DoctorVisits { get; set; }
        public DbSet<Treatment> Treatments { get; set; }
        public DbSet<TreatmentMedication> TreatmentMedications { get; set; } // join table
        public DbSet<VitalSigns> VitalSigns { get; set; }
        public DbSet<PrescriptionMedication> PrescriptionMedications { get; set; } //Added

        // Patient Management subsystem
        public DbSet<Discharge> Discharges { get; set; }
        public DbSet<Patient> Patients { get; set; }
        public DbSet<PatientAdmission> PatientAdmissions { get; set; }
        public DbSet<PatientMovement> PatientMovements { get; set; }
        public DbSet<PatientMedicalHistory> MedicalHistories { get; set; }

        public DbSet<VisitNote> VisitNotes { get; set; }
        public DbSet<Instruction> Instructions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ========== MANY-TO-MANY RELATIONSHIPS ==========

            // Patient ↔ Allergy (Many-to-Many)
            builder.Entity<PatientAllergy>()
                .HasKey(pa => pa.PatientAllergyId);

            builder.Entity<PatientAllergy>()
                .HasOne(pa => pa.Patient)
                .WithMany(p => p.PatientAllergies)
                .HasForeignKey(pa => pa.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PatientAllergy>()
                .HasOne(pa => pa.Allergy)
                .WithMany(a => a.PatientAllergies)
                .HasForeignKey(pa => pa.AllergyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Patient ↔ MedicalCondition (Many-to-Many)
            builder.Entity<PatientMedicalCondition>()
                .HasKey(pmc => pmc.PatientMedicalConditionId);

            builder.Entity<PatientMedicalCondition>()
                .HasOne(pmc => pmc.Patient)
                .WithMany(p => p.PatientMedicalConditions)
                .HasForeignKey(pmc => pmc.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PatientMedicalCondition>()
                .HasOne(pmc => pmc.MedicalCondition)
                .WithMany(mc => mc.PatientMedicalConditions)
                .HasForeignKey(pmc => pmc.MedicalConditionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Patient ↔ Medication (Many-to-Many) - NEW
            builder.Entity<PatientMedication>()
                .HasKey(pm => pm.PatientMedicationId);

            builder.Entity<PatientMedication>()
                .HasOne(pm => pm.Patient)
                .WithMany(p => p.PatientMedications)
                .HasForeignKey(pm => pm.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PatientMedication>()
                .HasOne(pm => pm.Medication)
                .WithMany(m => m.PatientMedications)
                .HasForeignKey(pm => pm.MedicationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PatientMedication>()
                .HasOne(pm => pm.Employee)
                .WithMany(e => e.PatientMedications)
                .HasForeignKey(pm => pm.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // ========== EXISTING RELATIONSHIPS (UPDATED) ==========

            // Remove the old one-to-many relationships for Patient-Allergy and Patient-MedicalCondition
            // These should be commented out or removed since we're using many-to-many now

            // PatientAdmission -> Discharge
            builder.Entity<Discharge>()
                .HasOne(d => d.PatientAdmission)
                .WithMany(a => a.Discharges)
                .HasForeignKey(d => d.AdmissionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PatientAdmission>()
                .HasOne(pa => pa.Bed)
                .WithMany(b => b.PatientAdmissions)
                .HasForeignKey(pa => pa.BedId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Ward & Room (1:M)
            builder.Entity<Ward>()
                .HasMany(w => w.Rooms)
                .WithOne(r => r.Ward)
                .HasForeignKey(r => r.WardId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // Room & Bed (1:M)
            builder.Entity<Room>()
                .HasMany(r => r.Beds)
                .WithOne(b => b.Room)
                .HasForeignKey(b => b.RoomId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // Employee & Consumables (1:M)
            builder.Entity<Employee>()
                .HasMany(e => e.Consumables)
                .WithOne(c => c.Employee)
                .HasForeignKey(c => c.EmployeeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // Employee & Medications (1:M) → stock management
            builder.Entity<Employee>()
                .HasMany(e => e.Medications)
                .WithOne(m => m.Employee)
                .HasForeignKey(m => m.EmployeeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // Employee & Allergies (1:M) - This stays for employee's own allergies
            builder.Entity<Employee>()
                .HasMany(e => e.Allergies)
                .WithOne(a => a.Employee)
                .HasForeignKey(a => a.EmployeeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // Employee & PatientMedications (1:M) - NEW
            builder.Entity<Employee>()
                .HasMany(e => e.PatientMedications)
                .WithOne(pm => pm.Employee)
                .HasForeignKey(pm => pm.EmployeeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Employee & PatientAdmissions (1:M)
            builder.Entity<Employee>()
                .HasMany(e => e.PatientAdmissions)
                .WithOne(pa => pa.Employee)
                .HasForeignKey(pa => pa.EmployeeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // Employee & ScheduleVisits (1:M)
            builder.Entity<Employee>()
                .HasMany(e => e.ScheduleVisits)
                .WithOne(sv => sv.Employee)
                .HasForeignKey(sv => sv.EmployeeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // Employee & Prescriptions (1:M)
            builder.Entity<Employee>()
                .HasMany(e => e.Prescriptions)
                .WithOne(p => p.Employee)
                .HasForeignKey(p => p.EmployeeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // Employee & ConsumablesRequests (1:M)
            builder.Entity<Employee>()
                .HasMany(e => e.ConsumablesRequests)
                .WithOne(cr => cr.Employee)
                .HasForeignKey(cr => cr.EmployeeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // Employee & VitalSigns (1:M)
            builder.Entity<Employee>()
                .HasMany(e => e.VitalSigns)
                .WithOne(vs => vs.Employee)
                .HasForeignKey(vs => vs.EmployeeId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // Ward & Consumables (1:M)
            builder.Entity<Ward>()
                .HasMany(w => w.Consumables)
                .WithOne(c => c.Ward)
                .HasForeignKey(c => c.WardId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // Ward & Employees (1:M)
            builder.Entity<Ward>()
                .HasMany(w => w.Employees)
                .WithOne(e => e.Ward)
                .HasForeignKey(e => e.WardId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // ConsumablesRequest & Consumables (1:M)
            builder.Entity<ConsumablesRequest>()
                .HasMany(cr => cr.Consumables)
                .WithOne(c => c.ConsumablesRequest)
                .HasForeignKey(c => c.RequestId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Patient & Prescriptions (1:M)
            builder.Entity<Patient>()
                .HasMany(p => p.Prescriptions)
                .WithOne(pr => pr.Patient)
                .HasForeignKey(pr => pr.PatientId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // Prescription & PrescriptionMedications (Many-to-Many)
            builder.Entity<PrescriptionMedication>()
                .HasKey(pm => new { pm.PrescriptionId, pm.MedicationId });

            builder.Entity<PrescriptionMedication>()
                .HasOne(pm => pm.Prescription)
                .WithMany(p => p.PrescriptionMedications)
                .HasForeignKey(pm => pm.PrescriptionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<PrescriptionMedication>()
                .HasOne(pm => pm.Medication)
                .WithMany(m => m.PrescriptionMedications)
                .HasForeignKey(pm => pm.MedicationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Treatment & TreatmentMedications (1:M)
            builder.Entity<Treatment>()
                .HasMany(t => t.TreatmentMedications)
                .WithOne(tm => tm.Treatment)
                .HasForeignKey(tm => tm.TreatmentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // Medication & TreatmentMedications (1:M)
            builder.Entity<Medication>()
                .HasMany(m => m.TreatmentMedications)
                .WithOne(tm => tm.Medication)
                .HasForeignKey(tm => tm.MedicationId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Patient & DoctorVisits (1:M)
            builder.Entity<Patient>()
                .HasMany(p => p.DoctorVisits)
                .WithOne(dv => dv.Patient)
                .HasForeignKey(dv => dv.PatientId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // Patient & Treatments (1:M)
            builder.Entity<Patient>()
                .HasMany(p => p.Treatments)
                .WithOne(t => t.Patient)
                .HasForeignKey(t => t.PatientId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // Patient & Discharges (1:M)
            builder.Entity<Patient>()
                .HasMany(p => p.Discharges)
                .WithOne(d => d.Patient)
                .HasForeignKey(d => d.PatientId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // Patient & PatientAdmissions (1:M)
            builder.Entity<Patient>()
                .HasMany(p => p.PatientAdmissions)
                .WithOne(pa => pa.Patient)
                .HasForeignKey(pa => pa.PatientId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // PatientAdmission & PatientMovements (1:M)
            builder.Entity<PatientAdmission>()
                .HasMany(pa => pa.PatientMovements)
                .WithOne(pm => pm.PatientAdmission)
                .HasForeignKey(pm => pm.AdmissionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // Patient & MedicalHistory (1:M)
            builder.Entity<PatientMedicalHistory>()
                .HasOne(mh => mh.Patient)
                .WithMany(p => p.MedicalHistories)
                .HasForeignKey(mh => mh.PatientId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            // PatientAdmission -> Discharge
            builder.Entity<Discharge>()
                .HasOne(d => d.PatientAdmission)
                .WithMany(a => a.Discharges)
                .HasForeignKey(d => d.AdmissionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<PatientMovement>()
                .HasOne(pm => pm.Bed)
                .WithMany(b => b.PatientMovements)
                .HasForeignKey(pm => pm.BedId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // Patient & VitalSigns (1:M)
            builder.Entity<Patient>()
                .HasMany(e => e.VitalSigns)
                .WithOne(cr => cr.Patient)
                .HasForeignKey(cr => cr.PatientId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);
        }

    }
}
