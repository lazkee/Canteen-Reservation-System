using System;
using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Domain.Attributes;


namespace Domain.Models
{
    public class Reservation
    {
        [Key]
        public Guid ReservationId { get; set; }

        [Required]
        public Guid StudentId { get; set; }

        [Required]
        public Guid CanteenId { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required]
        public TimeOnly Time { get; set; }

        [Required]
        [RestrictedValues(30, 60, ErrorMessage = "Duration must be either 30 or 60 minutes")]
        public uint Duration { get; set; }

        [Required]
        public ReservationStatus Status { get; set; }

        public Student? Student { get; set; }
        public Canteen? Canteen { get; set; }

        public Reservation()
        {
            ReservationId = Guid.NewGuid();
            Status = ReservationStatus.Active;
        }

        public Reservation(Guid studentId, Guid canteenId, DateOnly date, TimeOnly time, uint duration)
            : this()
        {
            StudentId = studentId;
            CanteenId = canteenId;
            Date = date;
            Time = time;
            Duration = duration;
        }
    }
}