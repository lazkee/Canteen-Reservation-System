using System;
using System.ComponentModel.DataAnnotations;

namespace Application.Reservations
{
    public sealed record CreateReservationRequest
    {
        [Required]
        public string StudentId { get; init; } = string.Empty;

        [Required]
        public string CanteenId { get; init; } = string.Empty;

        [Required]
        [RegularExpression(@"^\d{4}-\d{2}-\d{2}$",
            ErrorMessage = "Date must be in format yyyy-MM-dd.")]
        public string Date { get; init; } = string.Empty;

        [Required]
        [RegularExpression(@"^\d{2}:(00|30)$",
            ErrorMessage = "Time must be in format HH:mm on full or half hour.")]
        public string Time { get; init; } = string.Empty;

        [Required]
        [Range(30, 60, ErrorMessage = "Duration must be either 30 or 60 minutes.")]
        public uint Duration { get; init; }
    }


    public sealed record ReservationResponse
    {
        public string Id { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;     
        public string StudentId { get; init; } = string.Empty;
        public string CanteenId { get; init; } = string.Empty;
        public string Date { get; init; } = string.Empty;       
        public string Time { get; init; } = string.Empty;      
        public uint Duration { get; init; }
    }

}