using System;
namespace Application.Students
{
    using System.ComponentModel.DataAnnotations;

    public sealed record CreateStudentRequest
    {
        [Required(ErrorMessage = "Name is required")]
        [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(320, ErrorMessage = "Email cannot exceed 320 characters")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "IsAdmin is required")]
        public bool IsAdmin { get; set; }
    }


    public sealed record StudentResponse
	{
		public string Id { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public bool IsAdmin { get; set; } 
	}
}

