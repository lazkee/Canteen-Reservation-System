using System;
namespace Application.Auth
{
	public interface IAuthService
	{
		Task<bool> IsStudentAdminAsync(string studentId);
	}
}

