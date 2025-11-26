using Application.Canteens;
using Domain.Models;

public class CanteenValidator
{
    // For creation (full DTO)
    public IEnumerable<string> Validate(CreateCanteenRequest request)
    {
        return ValidateWorkingHours(request.WorkingHours);
    }

    // For update (partial DTO)
    public IEnumerable<string> Validate(UpdateCanteenRequest request)
    {
        if (request.WorkingHours is null)
            return Enumerable.Empty<string>();

        return ValidateWorkingHours(request.WorkingHours);
    }

    
    private IEnumerable<string> ValidateWorkingHours(List<WorkingHourDto> workingHours)
    {
        var errors = new List<string>();
        var mealSet = new HashSet<string>();

        foreach (WorkingHourDto wh in workingHours)
        {
            if (!mealSet.Add(wh.Meal.ToUpper()))
                errors.Add($"Duplicate meal: {wh.Meal.ToUpper()}");
        }

        var intervals = workingHours.Select(wh => new
        {
            Start = TimeOnly.Parse(wh.From),
            End = TimeOnly.Parse(wh.To),
            Meal = wh.Meal
        }).ToList();

        for (int i = 0; i < intervals.Count; i++)
        {
            for (int j = i + 1; j < intervals.Count; j++)
            {
                if (intervals[i].Start < intervals[j].End && intervals[j].Start < intervals[i].End)
                {
                    errors.Add($"Working hours overlap: {intervals[i].Meal} ({intervals[i].Start}-{intervals[i].End}) and {intervals[j].Meal} ({intervals[j].Start}-{intervals[j].End})");
                }
            }
        }

        return errors;
    }

    
    public IEnumerable<string> ValidateStatusRequest(
        DateOnly startDate,
        DateOnly endDate,
        TimeOnly startTime,
        TimeOnly endTime,
        uint duration)
    {
        var errors = new List<string>();

        if (startDate > endDate)
            errors.Add("startDate must be less than or equal to endDate.");

        if (startTime >= endTime)
            errors.Add("startTime must be less than endTime.");

        if (duration is not (30 or 60))
            errors.Add("duration must be either 30 or 60 minutes.");

        
        if (endDate.DayNumber - startDate.DayNumber > 31)
            errors.Add("Date range is too large. Maximum allowed is 31 days.");


        return errors;
    }
}
