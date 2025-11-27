using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Application.Students;
using Domain.Models;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CanteenReservationSystem.Tests
{
    public class StudentServiceTests
    {
        private static CanteenDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<CanteenDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new CanteenDbContext(options);
        }

        [Fact]
        public async Task CreateAsync_ValidRequest_PersistsStudent()
        {
            using var context = CreateDbContext();
            var service = new StudentService(context);

            var request = new CreateStudentRequest
            {
                Name = "Marko",
                Email = "marko@example.com",
                IsAdmin = true
            };

            var response = await service.CreateAsync(request);

            response.Id.Should().NotBeNullOrEmpty();
            response.Name.Should().Be("Marko");
            response.Email.Should().Be("marko@example.com");
            response.IsAdmin.Should().BeTrue();

            var inDb = await context.Students.SingleAsync();
            inDb.Name.Should().Be("Marko");
            inDb.Email.Should().Be("marko@example.com");
            inDb.IsAdmin.Should().BeTrue();
        }

        [Fact]
        public async Task CreateAsync_DuplicateEmail_ThrowsValidationException()
        {
            using var context = CreateDbContext();

            context.Students.Add(new Student("Existing", "dup@example.com", false));
            await context.SaveChangesAsync();

            var service = new StudentService(context);

            var request = new CreateStudentRequest
            {
                Name = "New",
                Email = "dup@example.com",
                IsAdmin = false
            };

            Func<Task> act = async () => await service.CreateAsync(request);

            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task GetByIdAsync_InvalidGuid_ReturnsNull()
        {
            using var context = CreateDbContext();
            var service = new StudentService(context);

            var result = await service.GetByIdAsync("not-a-guid");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ReturnsNull()
        {
            using var context = CreateDbContext();
            var service = new StudentService(context);

            var result = await service.GetByIdAsync(Guid.NewGuid().ToString());

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_Found_ReturnsMappedResponse()
        {
            using var context = CreateDbContext();

            var student = new Student("Ana", "ana@example.com", isAdmin: false);
            context.Students.Add(student);
            await context.SaveChangesAsync();

            var service = new StudentService(context);

            var result = await service.GetByIdAsync(student.StudentId.ToString());

            result.Should().NotBeNull();
            result!.Id.Should().Be(student.StudentId.ToString());
            result.Name.Should().Be("Ana");
            result.Email.Should().Be("ana@example.com");
            result.IsAdmin.Should().BeFalse();
        }
    }
}
