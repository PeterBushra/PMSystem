using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Jobick.Models;

public partial class AdhmmcPmContext : DbContext
{
    public AdhmmcPmContext()
    {
    }

    public AdhmmcPmContext(DbContextOptions<AdhmmcPmContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<Task> Tasks { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Only configure if options are not already set (for design-time tools)
        if (!optionsBuilder.IsConfigured)
        {
            // Use configuration-based connection string
            optionsBuilder.UseSqlServer(
                "Name=DefaultConnection",
                sqlOptions => sqlOptions.EnableRetryOnFailure()
                );
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("Project");

            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.Description).HasMaxLength(4000);
            entity.Property(e => e.DescriptionAr).HasMaxLength(4000);
            entity.Property(e => e.Name).HasMaxLength(250);
            entity.Property(e => e.NameAr).HasMaxLength(250);
            entity.Property(e => e.ProjectGoal).HasMaxLength(2500);
            entity.Property(e => e.ResponsibleForImplementing).HasMaxLength(150);
            entity.Property(e => e.StrategicGoal).HasMaxLength(250);
            entity.Property(e => e.StrategicProgramme).HasMaxLength(250);
            entity.Property(e => e.SystemOwner).HasMaxLength(150);
            entity.Property(e => e.TotalCost).HasColumnType("decimal(18, 4)");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Projects)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Project_User");
        });

        modelBuilder.Entity<Task>(entity =>
        {
            entity.ToTable("Task");

            entity.Property(e => e.AttachementFilePath).HasMaxLength(1050);
            entity.Property(e => e.Cost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedDate).HasDefaultValueSql("(getutcdate())");
            entity.Property(e => e.DefinationOfDone).HasMaxLength(4000);
            entity.Property(e => e.DepartmentResponsible).HasMaxLength(50);
            entity.Property(e => e.DoneRatio).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.ImplementorDepartment).HasMaxLength(50);
            entity.Property(e => e.StageName).HasMaxLength(50);
            entity.Property(e => e.StageNameAr).HasMaxLength(50);
            entity.Property(e => e.Task1)
                .HasMaxLength(50)
                .HasColumnName("Task");
            entity.Property(e => e.TaskAr).HasMaxLength(50);
            entity.Property(e => e.Weight).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Task_User");

            entity.HasOne(d => d.Project).WithMany(p => p.Tasks)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Task_Project");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.Property(e => e.Email).HasMaxLength(250);
            entity.Property(e => e.Password).HasMaxLength(500);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
