using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Domain.Models
{
    public class Canteen
    {
        [Key]
        public Guid CanteenId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [Range(1, 10000)]
        public uint Capacity { get; set; }

        
        public List<WorkingHour> WorkingHours { get; set; } = new();

        public Canteen()
        {
            CanteenId = Guid.NewGuid();
            WorkingHours = new List<WorkingHour>();
        }

        public Canteen(string name, string location, uint capacity) : this()
        {
            Name = name;
            Location = location;
            Capacity = capacity;
        }
    }
}