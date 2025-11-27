using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Application.Canteens;
using Domain.Enums;
using Domain.Models;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CanteenReservationSystem.Tests
{
    public class CanteenServiceTests
    {
        private static CanteenDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<CanteenDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new CanteenDbContext(options);
        }

        private static Canteen SeedCanteenWithReservations(
            CanteenDbContext context,
            int activeReservations,
            int cancelledReservations)
        {
            var canteen = new Canteen("Test", "Loc", 100);
            context.Canteens.Add(canteen);

            // future date & time so reservations are in the future
            var future = DateTime.UtcNow.AddDays(1);
            var date = DateOnly.FromDateTime(future);
            var time = TimeOnly.FromDateTime(future);

            for (int i = 0; i < activeReservations; i++)
            {
                context.Reservations.Add(new Reservation
                {
                    ReservationId = Guid.NewGuid(),
                    CanteenId = canteen.CanteenId,
                    Date = date,
                    Time = time,
                    Duration = 30,
                    Status = ReservationStatus.Active,
                    StudentId = Guid.NewGuid()
                });
            }

            for (int i = 0; i < cancelledReservations; i++)
            {
                context.Reservations.Add(new Reservation
                {
                    ReservationId = Guid.NewGuid(),
                    CanteenId = canteen.CanteenId,
                    Date = date,
                    Time = time.AddMinutes(60),
                    Duration = 30,
                    Status = ReservationStatus.Cancelled,
                    StudentId = Guid.NewGuid()
                });
            }

            context.SaveChanges();
            return canteen;
        }

        [Fact]
        public async Task DeleteAsync_InvalidGuid_ReturnsFalse_AndDoesNotChangeDb()
        {
            using var context = CreateDbContext();
            var service = new CanteenService(context);

            var result = await service.DeleteAsync("not-a-guid");

            result.Should().BeFalse();
            context.Canteens.Should().BeEmpty();
            context.Reservations.Should().BeEmpty();
        }

        [Fact]
        public async Task DeleteAsync_CanteenNotFound_ReturnsFalse()
        {
            using var context = CreateDbContext();
            var service = new CanteenService(context);

            var result = await service.DeleteAsync(Guid.NewGuid().ToString());

            result.Should().BeFalse();
        }

        [Fact]
        public async Task DeleteAsync_CancelsActiveReservations_AndDeletesCanteen()
        {
            using var context = CreateDbContext();
            var canteen = SeedCanteenWithReservations(context, activeReservations: 3, cancelledReservations: 2);
            var service = new CanteenService(context);

            var result = await service.DeleteAsync(canteen.CanteenId.ToString());

            result.Should().BeTrue();

            var canteenInDb = await context.Canteens
                .FirstOrDefaultAsync(c => c.CanteenId == canteen.CanteenId);
            canteenInDb.Should().BeNull();

            
            var remainingForCanteen = await context.Reservations
                .AnyAsync(r => r.CanteenId == canteen.CanteenId &&
                               r.Status != ReservationStatus.Cancelled);

            remainingForCanteen.Should().BeFalse();
        }


        [Fact]
        public async Task DeleteAsync_DoesNotCancelReservations_FromOtherCanteens()
        {
            using var context = CreateDbContext();

            var target = SeedCanteenWithReservations(context, activeReservations: 2, cancelledReservations: 0);

            var other = new Canteen("Other", "Loc", 50);
            context.Canteens.Add(other);

            var future = DateTime.UtcNow.AddDays(1);
            var otherDate = DateOnly.FromDateTime(future);
            var otherTime = TimeOnly.FromDateTime(future);

            context.Reservations.Add(new Reservation
            {
                ReservationId = Guid.NewGuid(),
                CanteenId = other.CanteenId,
                Date = otherDate,
                Time = otherTime,
                Duration = 30,
                Status = ReservationStatus.Active,
                StudentId = Guid.NewGuid()
            });

            await context.SaveChangesAsync();

            var service = new CanteenService(context);

            var result = await service.DeleteAsync(target.CanteenId.ToString());

            result.Should().BeTrue();

            (await context.Canteens.AnyAsync(c => c.CanteenId == other.CanteenId))
                .Should().BeTrue();

            var otherReservation = await context.Reservations
                .FirstAsync(r => r.CanteenId == other.CanteenId);
            otherReservation.Status.Should().Be(ReservationStatus.Active);
        }

        [Fact]
        public async Task DeleteAsync_WhenNoReservations_DeletesCanteenSuccessfully()
        {
            using var context = CreateDbContext();

            var canteen = new Canteen("NoReservations", "Loc", 100);
            context.Canteens.Add(canteen);
            await context.SaveChangesAsync();

            var service = new CanteenService(context);

            var result = await service.DeleteAsync(canteen.CanteenId.ToString());

            result.Should().BeTrue();
            (await context.Canteens.AnyAsync(c => c.CanteenId == canteen.CanteenId))
                .Should().BeFalse();
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_PersistsCanteenWithWorkingHours()
        {
            using var context = CreateDbContext();
            var service = new CanteenService(context);

            var request = new CreateCanteenRequest
            {
                Name = "Tri kostura",
                Location = "Obilićev venac 4",
                Capacity = 100,
                WorkingHours = new List<WorkingHourDto>
                {
                    new() { Meal = "breakfast", From = "08:00", To = "10:00" },
                    new() { Meal = "lunch",     From = "11:00", To = "13:00" }
                }
            };

            var response = await service.CreateAsync(request);

            response.Name.Should().Be("Tri kostura");
            response.WorkingHours.Should().HaveCount(2);
            (await context.Canteens.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task CreateAsync_DuplicateName_IgnoresCase_ThrowsValidationException()
        {
            using var context = CreateDbContext();
            var existing = new Canteen("Tri kostura", "Loc", 50);
            context.Canteens.Add(existing);
            await context.SaveChangesAsync();

            var service = new CanteenService(context);

            var request = new CreateCanteenRequest
            {
                Name = "tri KOSTURA", // same name different case
                Location = "Other",
                Capacity = 100,
                WorkingHours = new List<WorkingHourDto>()
            };

            Func<Task> act = async () => await service.CreateAsync(request);

            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task UpdateAsync_InvalidId_ReturnsNull()
        {
            using var context = CreateDbContext();
            var service = new CanteenService(context);

            var request = new UpdateCanteenRequest { Name = "New name" };

            var result = await service.UpdateAsync("not-a-guid", request);

            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_CanteenNotFound_ReturnsNull()
        {
            using var context = CreateDbContext();
            var service = new CanteenService(context);

            var request = new UpdateCanteenRequest { Name = "New name" };

            var result = await service.UpdateAsync(Guid.NewGuid().ToString(), request);

            result.Should().BeNull();
        }

        [Fact]
        public async Task UpdateAsync_UpdatesAllFields_WhenProvided()
        {
            using var context = CreateDbContext();

            var canteen = new Canteen("Old", "OldLoc", 50);
            canteen.WorkingHours.Add(new WorkingHour
            {
                Meal = Meal.BREAKFAST,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(10, 0)
            });
            context.Canteens.Add(canteen);
            await context.SaveChangesAsync();

            var service = new CanteenService(context);

            var request = new UpdateCanteenRequest
            {
                Name = "New",
                Location = "NewLoc",
                Capacity = 100,
                WorkingHours = new List<WorkingHourDto>
                {
                    new() { Meal = "lunch", From = "11:00", To = "13:00" }
                }
            };

            var response = await service.UpdateAsync(canteen.CanteenId.ToString(), request);

            response.Should().NotBeNull();
            response!.Name.Should().Be("New");
            response.Location.Should().Be("NewLoc");
            response.Capacity.Should().Be(100);
            response.WorkingHours.Should().HaveCount(1);
            response.WorkingHours[0].Meal.Should().Be("lunch");
        }

        [Fact]
        public async Task UpdateAsync_PartialUpdate_DoesNotOverwriteUnspecifiedFields()
        {
            using var context = CreateDbContext();

            var canteen = new Canteen("Old", "OldLoc", 50);
            canteen.WorkingHours.Add(new WorkingHour
            {
                Meal = Meal.BREAKFAST,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(10, 0)
            });
            context.Canteens.Add(canteen);
            await context.SaveChangesAsync();

            var service = new CanteenService(context);

            var request = new UpdateCanteenRequest
            {
                Name = "OnlyNameChanged"
                // Location, Capacity, WorkingHours left null on purpose
            };

            var response = await service.UpdateAsync(canteen.CanteenId.ToString(), request);

            response.Should().NotBeNull();
            response!.Name.Should().Be("OnlyNameChanged");
            response.Location.Should().Be("OldLoc");   // unchanged
            response.Capacity.Should().Be(50);         // unchanged
            response.WorkingHours.Should().HaveCount(1); // unchanged working hours
        }

        [Fact]
        public async Task GetRemainingCapacityForCanteenAsync_ComputesRemainingCapacityFromReservations()
        {
            using var context = CreateDbContext();

            var canteen = new Canteen("Test", "Loc", capacity: 10);
            canteen.WorkingHours.Add(new WorkingHour
            {
                Meal = Meal.LUNCH,
                StartTime = new TimeOnly(11, 0),
                EndTime = new TimeOnly(13, 0)
            });
            context.Canteens.Add(canteen);

            var future = DateTime.UtcNow.AddDays(1);
            var date = DateOnly.FromDateTime(future);
            var time = new TimeOnly(12, 0);

            // 3 active reservations overlapping 12:00–12:30
            for (int i = 0; i < 3; i++)
            {
                context.Reservations.Add(new Reservation
                {
                    ReservationId = Guid.NewGuid(),
                    CanteenId = canteen.CanteenId,
                    Date = date,
                    Time = time,
                    Duration = 30,
                    Status = ReservationStatus.Active,
                    StudentId = Guid.NewGuid()
                });
            }

            await context.SaveChangesAsync();

            var service = new CanteenService(context);

            var status = await service.GetRemainingCapacityForCanteenAsync(
                canteen.CanteenId.ToString(),
                startDate: date,
                endDate: date,
                startTime: new TimeOnly(11, 0),
                endTime: new TimeOnly(13, 0),
                duration: 30);

            status.Should().NotBeNull();
            status!.CanteenId.Should().Be(canteen.CanteenId.ToString());

            var lunchSlot = status.Slots
                .First(s => s.Meal == "lunch" && s.StartTime == "12:00");

            lunchSlot.RemainingCapacity.Should().Be(7); // 10 - 3
        }

        [Fact]
        public async Task GetRemainingCapacityForCanteenAsync_DurationZero_ThrowsArgumentException()
        {
            using var context = CreateDbContext();

            var canteen = new Canteen("Test", "Loc", 10);
            context.Canteens.Add(canteen);
            await context.SaveChangesAsync();

            var service = new CanteenService(context);

            var future = DateTime.UtcNow.AddDays(1);
            var date = DateOnly.FromDateTime(future);

            Func<Task> act = async () =>
                await service.GetRemainingCapacityForCanteenAsync(
                    canteen.CanteenId.ToString(),
                    startDate: date,
                    endDate: date,
                    startTime: new TimeOnly(11, 0),
                    endTime: new TimeOnly(13, 0),
                    duration: 0);

            await act.Should().ThrowAsync<ArgumentException>();
        }

    }
}
