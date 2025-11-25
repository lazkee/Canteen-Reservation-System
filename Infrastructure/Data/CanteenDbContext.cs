using System;
using Microsoft.EntityFrameworkCore;
using Domain.Models;
namespace Infrastructure.Data
{
	public class CanteenDbContext : DbContext
	{
		public DbSet<Student> Students { get; set; }
		public DbSet<Canteen> Canteens { get; set; }
		public DbSet<Reservation> Reservations { get; set; }

		public CanteenDbContext(DbContextOptions<CanteenDbContext> options):base()
		{
		}

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

			//Student config
			modelBuilder.Entity<Student>(entity =>
			{
				entity.HasKey(s => s.StudentId);
				entity.HasIndex(s => s.Email).IsUnique();
			});


			//Canteen(with working hours) config
			modelBuilder.Entity<Canteen>(entity =>
			{
				entity.HasKey(c => c.CanteenId);
				entity.OwnsMany(c => c.WorkingHours, wh =>
				{
					wh.Property(w => w.Meal).IsRequired();
					wh.Property(w => w.StartTime).IsRequired();
					wh.Property(w => w.EndTime).IsRequired();
				});
			});

			//Reservation config
			modelBuilder.Entity<Reservation>(entity =>
			{
				entity.HasKey(r => r.ReservationId);

				entity.HasOne(r => r.Student)
				.WithMany()
				.HasForeignKey(r => r.CanteenId)
				.OnDelete(DeleteBehavior.Cascade);

				entity.HasOne(c => c.Canteen)
				.WithMany()
				.HasForeignKey(c => c.CanteenId)
				.OnDelete(DeleteBehavior.Cascade);
			});
        }
    }
}

