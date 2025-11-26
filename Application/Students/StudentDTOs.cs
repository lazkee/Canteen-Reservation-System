using System;
namespace Application.Students
{
	public sealed record CreateStudentRequest
	{
		public string Name { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public bool isAdmin { get; set; }
	}

	public sealed record StudentResponse
	{
		public string Id { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public bool IsAdmin { get; set; } 
	}
}

