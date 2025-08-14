using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using renjibackend.Models;

namespace renjibackend.Data;

public partial class RenjiDbContext : DbContext
{
    public RenjiDbContext()
    {
    }

    public RenjiDbContext(DbContextOptions<RenjiDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Accident> Accidents { get; set; }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<IncidentReport> IncidentReports { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-P3T6S9M\\SQLEXPRESS;Database=RenjiDB;Trusted_Connection=True;Encrypt=False;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Accident>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Accident__3214EC07F9566D4D");

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Departme__3214EC075CC035F2");

            entity.ToTable("Department");

            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<IncidentReport>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Incident__3214EC07E2C26150");

            entity.Property(e => e.AttachmentPath).HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.LastUpdated).HasColumnType("datetime");
            entity.Property(e => e.Location).HasMaxLength(255);
            entity.Property(e => e.ReportedDate).HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(100);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__User__3214EC07F45D57B0");

            entity.ToTable("User");

            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Department).WithMany(p => p.Users)
                .HasForeignKey(d => d.DepartmentId)
                .HasConstraintName("FK_User_Department");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
