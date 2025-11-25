using System;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Models
{
    public class WorkingHour
    {
        [Required]
        public Meal Meal { get; set; }

        [Required]
        public TimeOnly StartTime { get; set; }

        [Required]
        public TimeOnly EndTime { get; set; }

        public WorkingHour()
        {
        }

        public WorkingHour(Meal meal, TimeOnly startTime, TimeOnly endTime)
        {
            Meal = meal;
            StartTime = startTime;
            EndTime = endTime;
        }
    }
}