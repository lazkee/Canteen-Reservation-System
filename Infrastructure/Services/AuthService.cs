using System;
using Infrastructure.Data;
using Application.Auth;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services
{
	public class AuthService: IAuthService
	{
        private readonly CanteenDbContext _context;
		public AuthService(CanteenDbContext context)
		{
            _context = context;
		}

        public async Task<bool> IsStudentAdminAsync(string studentId)
        {
            if (!Guid.TryParse(studentId, out var guid))
                return false;

            Student student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == guid);

            return student != null && student.IsAdmin;

        }
        
    }
}

