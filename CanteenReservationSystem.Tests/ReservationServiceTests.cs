using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Application.Reservations;
using Domain.Enums;
using Domain.Models;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CanteenReservationSystem.Tests
{
    public class ReservationServiceTests
    {
        private static CanteenDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<CanteenDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new CanteenDbContext(options);
        }

        private static (Canteen canteen, Student student, DateOnly date, TimeOnly time)
            SeedCanteenAndStudent(CanteenDbContext context, uint capacity = 10)
        {
            var canteen = new Canteen("Test", "Loc", capacity);
            canteen.WorkingHours.Add(new WorkingHour
            {
                Meal = Meal.LUNCH,
                StartTime = new TimeOnly(11, 0),
                EndTime = new TimeOnly(14, 0)
            });
            var student = new Student("Marko", "marko@example.com", false);

            context.Canteens.Add(canteen);
            context.Students.Add(student);
            context.SaveChanges();

            var future = DateTime.UtcNow.AddDays(1);
            var date = DateOnly.FromDateTime(future);
            var time = new TimeOnly(12, 0);

            return (canteen, student, date, time);
        }

        private static CreateReservationRequest BuildValidRequest(
            Student student, Canteen canteen, DateOnly date, TimeOnly time, int duration = 30)
        {
            return new CreateReservationRequest
            {
                StudentId = student.StudentId.ToString(),
                CanteenId = canteen.CanteenId.ToString(),
                Date = date.ToString("yyyy-MM-dd"),
                Time = time.ToString("HH:mm"),
                Duration = (uint)duration
            };
        }

        // ---------- CreateAsync ----------

        [Fact]
        public async Task CreateAsync_ValidRequest_CreatesReservation()
        {
            using var context = CreateDbContext();
            var (canteen, student, date, time) = SeedCanteenAndStudent(context);
            var service = new ReservationService(context);

            var request = BuildValidRequest(student, canteen, date, time);

            var response = await service.CreateAsync(request);

            response.Should().NotBeNull();
            response!.Status.Should().Be(ReservationStatus.Active.ToString());
            response.StudentId.Should().Be(student.StudentId.ToString());
            response.CanteenId.Should().Be(canteen.CanteenId.ToString());

            var inDb = await context.Reservations.SingleAsync();
            inDb.Status.Should().Be(ReservationStatus.Active);
        }

        [Theory]
        [InlineData("bad-student-id", "valid", "2025-10-10", "12:00", 30)]
        [InlineData("guid", "bad-canteen-id", "2025-10-10", "12:00", 30)]
        [InlineData("guid", "guid", "bad-date", "12:00", 30)]
        [InlineData("guid", "guid", "2025-10-10", "bad-time", 30)]
        public async Task CreateAsync_InvalidParsing_ReturnsNull(
            string studentId, string canteenId, string date, string time, int duration)
        {
            using var context = CreateDbContext();
            var (canteen, student, futureDate, futureTime) = SeedCanteenAndStudent(context);
            var service = new ReservationService(context);

            var request = new CreateReservationRequest
            {
                StudentId = studentId == "guid" ? student.StudentId.ToString() : studentId,
                CanteenId = canteenId == "guid" ? canteen.CanteenId.ToString() : canteenId,
                Date = date == "2025-10-10" ? futureDate.ToString("yyyy-MM-dd") : date,
                Time = time == "12:00" ? futureTime.ToString("HH:mm") : time,
                Duration = (uint)duration
            };

            var result = await service.CreateAsync(request);

            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_PastDate_ReturnsNull()
        {
            using var context = CreateDbContext();
            var (canteen, student, _, time) = SeedCanteenAndStudent(context);
            var service = new ReservationService(context);

            var pastDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
            var request = BuildValidRequest(student, canteen, pastDate, time);

            var result = await service.CreateAsync(request);

            result.Should().BeNull();
        }

        [Theory]
        [InlineData(15)]
        [InlineData(90)]
        public async Task CreateAsync_InvalidDuration_ReturnsNull(int duration)
        {
            using var context = CreateDbContext();
            var (canteen, student, date, time) = SeedCanteenAndStudent(context);
            var service = new ReservationService(context);

            var request = BuildValidRequest(student, canteen, date, time, duration);

            var result = await service.CreateAsync(request);

            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_InvalidMinute_ReturnsNull()
        {
            using var context = CreateDbContext();
            var (canteen, student, date, _) = SeedCanteenAndStudent(context);
            var service = new ReservationService(context);

            var time = new TimeOnly(12, 15); // not 00 or 30
            var request = BuildValidRequest(student, canteen, date, time);

            var result = await service.CreateAsync(request);

            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_CanteenNotFound_ReturnsNull()
        {
            using var context = CreateDbContext();
            var (_, student, date, time) = SeedCanteenAndStudent(context);
            context.Canteens.RemoveRange(context.Canteens);
            await context.SaveChangesAsync();

            var service = new ReservationService(context);

            var request = new CreateReservationRequest
            {
                StudentId = student.StudentId.ToString(),
                CanteenId = Guid.NewGuid().ToString(),
                Date = date.ToString("yyyy-MM-dd"),
                Time = time.ToString("HH:mm"),
                Duration = 30
            };

            var result = await service.CreateAsync(request);

            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_OutsideWorkingHours_ReturnsNull()
        {
            using var context = CreateDbContext();
            var (canteen, student, date, _) = SeedCanteenAndStudent(context);
            var service = new ReservationService(context);

            var time = new TimeOnly(15, 0); // outside 11–14
            var request = BuildValidRequest(student, canteen, date, time);

            var result = await service.CreateAsync(request);

            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_StudentOverlappingReservation_ReturnsNull()
        {
            using var context = CreateDbContext();
            var (canteen, student, date, time) = SeedCanteenAndStudent(context);

            context.Reservations.Add(new Reservation(
                student.StudentId, canteen.CanteenId, date, time, 30)); // existing active
            await context.SaveChangesAsync();

            var service = new ReservationService(context);

            var request = BuildValidRequest(student, canteen, date, time);

            var result = await service.CreateAsync(request);

            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateAsync_CapacityReached_ReturnsNull()
        {
            using var context = CreateDbContext();
            var (canteen, student, date, time) = SeedCanteenAndStudent(context, capacity: 1);

            // one existing active reservation fills capacity
            context.Reservations.Add(new Reservation(
                student.StudentId, canteen.CanteenId, date, time, 30));
            await context.SaveChangesAsync();

            var anotherStudent = new Student("Ana", "ana@example.com", false);
            context.Students.Add(anotherStudent);
            await context.SaveChangesAsync();

            var service = new ReservationService(context);

            var request = BuildValidRequest(anotherStudent, canteen, date, time);

            var result = await service.CreateAsync(request);

            result.Should().BeNull();
        }

        // ---------- CancelAsync ----------

        [Fact]
        public async Task CancelAsync_InvalidIds_ReturnNull()
        {
            using var context = CreateDbContext();
            var service = new ReservationService(context);

            (await service.CancelAsync("bad", Guid.NewGuid().ToString())).Should().BeNull();
            (await service.CancelAsync(Guid.NewGuid().ToString(), "bad")).Should().BeNull();
        }

        [Fact]
        public async Task CancelAsync_NotFound_ReturnsNull()
        {
            using var context = CreateDbContext();
            var service = new ReservationService(context);

            var result = await service.CancelAsync(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());

            result.Should().BeNull();
        }

        [Fact]
        public async Task CancelAsync_StudentMismatch_ReturnsNull()
        {
            using var context = CreateDbContext();
            var (canteen, student, date, time) = SeedCanteenAndStudent(context);

            var reservation = new Reservation(student.StudentId, canteen.CanteenId, date, time, 30);
            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();

            var otherStudent = new Student("Ana", "ana@example.com", false);
            context.Students.Add(otherStudent);
            await context.SaveChangesAsync();

            var service = new ReservationService(context);

            var result = await service.CancelAsync(
                reservation.ReservationId.ToString(),
                otherStudent.StudentId.ToString());

            result.Should().BeNull();
            reservation.Status.Should().Be(ReservationStatus.Active);
        }

        [Fact]
        public async Task CancelAsync_ActiveReservation_SetsStatusCancelled()
        {
            using var context = CreateDbContext();
            var (canteen, student, date, time) = SeedCanteenAndStudent(context);

            var reservation = new Reservation(student.StudentId, canteen.CanteenId, date, time, 30);
            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();

            var service = new ReservationService(context);

            var result = await service.CancelAsync(
                reservation.ReservationId.ToString(),
                student.StudentId.ToString());

            result.Should().NotBeNull();
            result!.Status.Should().Be(ReservationStatus.Cancelled.ToString());

            reservation.Status.Should().Be(ReservationStatus.Cancelled);
        }

        [Fact]
        public async Task CancelAsync_AlreadyCancelled_DoesNotChangeStatus()
        {
            using var context = CreateDbContext();
            var (canteen, student, date, time) = SeedCanteenAndStudent(context);

            var reservation = new Reservation(student.StudentId, canteen.CanteenId, date, time, 30);
            reservation.Status = ReservationStatus.Cancelled;
            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();

            var service = new ReservationService(context);

            var result = await service.CancelAsync(
                reservation.ReservationId.ToString(),
                student.StudentId.ToString());

            result.Should().NotBeNull();
            reservation.Status.Should().Be(ReservationStatus.Cancelled);
        }

        // ---------- GetByIdAsync ----------

        [Fact]
        public async Task GetByIdAsync_InvalidGuid_ReturnsNull()
        {
            using var context = CreateDbContext();
            var service = new ReservationService(context);

            var result = await service.GetByIdAsync("bad-id");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ReturnsNull()
        {
            using var context = CreateDbContext();
            var service = new ReservationService(context);

            var result = await service.GetByIdAsync(Guid.NewGuid().ToString());

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_Found_ReturnsMappedResponse()
        {
            using var context = CreateDbContext();
            var (canteen, student, date, time) = SeedCanteenAndStudent(context);

            var reservation = new Reservation(student.StudentId, canteen.CanteenId, date, time, 30);
            context.Reservations.Add(reservation);
            await context.SaveChangesAsync();

            var service = new ReservationService(context);

            var result = await service.GetByIdAsync(reservation.ReservationId.ToString());

            result.Should().NotBeNull();
            result!.Id.Should().Be(reservation.ReservationId.ToString());
            result.Status.Should().Be(reservation.Status.ToString());
            result.StudentId.Should().Be(student.StudentId.ToString());
            result.CanteenId.Should().Be(canteen.CanteenId.ToString());
        }
    }
}
