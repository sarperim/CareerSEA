using CareerSEA.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CareerSEA.Data
{
    public class CareerSEADbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Prediction> Predictions { get; set; }
        public DbSet<Experience> Experiences { get; set; }
        public DbSet<ESCO_Occupation> ESCO_Occupations { get; set; }

        public CareerSEADbContext(DbContextOptions<CareerSEADbContext> options) : base(options) { } 

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.HasPostgresExtension("vector");
            // 1. User -> Experience (One-to-Many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Experiences)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade); // If User is deleted, delete experiences

            // 2. User -> Prediction (One-to-Many)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Predictions)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 3. Experience -> ESCO_Occupation (Many-to-One)
            // This was missing in your code
            modelBuilder.Entity<Experience>()
                .HasOne(e => e.ESCO_Occupation)
                .WithMany() // ESCO_Occupation doesn't have a list of Experiences in your class
                .HasForeignKey(e => e.ESCOId)
                .OnDelete(DeleteBehavior.Restrict); // Don't delete the Occupation just because an Experience is deleted

            // 4. Define Primary Key for ESCO (Because "ESCOId" isn't standard "Id")
            modelBuilder.Entity<ESCO_Occupation>()
                .HasKey(e => e.ESCOId);

            modelBuilder.Entity<ESCO_Occupation>()
                .Property(e => e.ESCOId).ValueGeneratedNever();

        }
    }
}
