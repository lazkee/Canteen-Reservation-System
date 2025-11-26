using System;
using Infrastructure.Data;
using Application.Students;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Services
{
	public class StudentService : IStudentService
	{

        private readonly CanteenDbContext _context;
		public StudentService(CanteenDbContext context)
		{
            _context = context;
		}

        public async Task<StudentResponse> CreateAsync(CreateStudentRequest request)
        {
            var exists = await _context.Students.AnyAsync(s => s.Email == request.Email);
            if (exists)
                throw new ValidationException("Email already in use.");

            Student student = new Student(request.Name, request.Email, request.IsAdmin);
            await _context.Students.AddAsync(student);
            await _context.SaveChangesAsync();
            

            return new StudentResponse
            {
                Id = student.StudentId.ToString(),
                Name = student.Name,
                Email = student.Email,
                IsAdmin = student.IsAdmin
            };
        }

        public async Task<StudentResponse> GetByIdAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;

            Student? student = await _context.Students.FindAsync(guid);
            if (student is null)
                return null;
            else
            {
                return new StudentResponse
                {
                    Id = student.StudentId.ToString(),
                    Name = student.Name,
                    Email = student.Email,
                    IsAdmin = student.IsAdmin

                };
            }
        }
    }
}

