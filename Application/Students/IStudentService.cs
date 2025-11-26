using System;
namespace Application.Students
{
	public interface IStudentService
	{
		Task<StudentResponse> CreateAsync(CreateStudentRequest request);
		Task<StudentResponse> GetByIdAsync(string id);
	}
}

