using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Domain.Models
{
    [Index(nameof(Email), IsUnique = true)]
    public class Student
    {
        [Key]
        public Guid StudentId { get; set; }

        [Required(ErrorMessage = "Name is required")]
        [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(320, ErrorMessage = "Email cannot exceed 320 characters")]
        public string Email { get; set; } = string.Empty;

        public bool IsAdmin { get; set; }

        public Student()
        {
            StudentId = Guid.NewGuid();
            IsAdmin = false;
        }

        public Student(string name, string email, bool isAdmin = false) : this()
        {
            Name = name;
            Email = email;
            IsAdmin = isAdmin;
        }
    }
}