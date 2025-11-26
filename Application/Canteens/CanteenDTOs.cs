using System.ComponentModel.DataAnnotations;
namespace Application.Canteens
{
	public sealed record CreateCanteenRequest
	{
        [Required(ErrorMessage = "Name is required")]
        [MaxLength(200, ErrorMessage = "Name cannot exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required")]
        [MaxLength(200, ErrorMessage = "Location cannot exceed 200 characters")]
        public string Location { get; set; } = string.Empty;

		[Required]
        [Range(1, 1000)]
        public uint Capacity { get; set; }

        [Required]
        public List<WorkingHourDto> WorkingHours { get; set; } = new();
    }

    public sealed record WorkingHourDto : IValidatableObject
    {
        [Required]
        [RegularExpression("^(?i)(BREAKFAST|LUNCH|DINNER)$", ErrorMessage = "Meal must be BREAKFAST, LUNCH, or DINNER.")]
        public string Meal { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^(?:[01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Time must be in HH:mm format (00:00–23:59).")]
        public string From { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^(?:[01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Time must be in HH:mm format (00:00–23:59).")]
        public string To { get; set; } = string.Empty;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (TimeOnly.TryParse(From, out var fromTime) && TimeOnly.TryParse(To, out var toTime))
            {
                if (fromTime >= toTime)
                {
                    yield return new ValidationResult(
                        $"{Meal}: from time must be earlier than to time.",
                        new[] { nameof(From), nameof(To) }
                    );
                }
            }
            else
            {
                yield return new ValidationResult(
                    $"{Meal}: invalid time format for From or To.",
                    new[] { nameof(From), nameof(To) }
                );
            }
        }

    }

    public sealed record UpdateCanteenRequest
    {
        [MaxLength(200)]
        public string? Name { get; set; }

        [MaxLength(200)]
        public string? Location { get; set; }

        [Range(1, 1000)]
        public uint? Capacity { get; set; }

        public List<WorkingHourDto>? WorkingHours { get; set; }
    }

    public sealed record CanteenResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public uint Capacity { get; set; }
        public List<WorkingHourDto> WorkingHours { get; set; } = new();
    }

    public sealed record CanteensResponse
    {
        public List<CanteenResponse> Canteens { get; set; } = new();
    }

    public sealed record CanteenStatus
    {
        public string CanteenId { get; set; } = string.Empty;
        public List<CanteenSlot> Slots { get; set; } = new();
    }

    public sealed record CanteenSlot
    {
        public string Date { get; set; } = string.Empty;
        public string Meal { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public uint RemainingCapacity { get; set; } 
    }
}

