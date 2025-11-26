using System;
using Application.Canteens;
using Infrastructure.Data;
using Domain.Models;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Services
{
    public class CanteenService : ICanteenService
    {
        private readonly CanteenDbContext _context;
        public CanteenService(CanteenDbContext context)
        {
            _context = context;
        }

        public async Task<CanteenResponse> CreateAsync(CreateCanteenRequest request)
        {
            var exists = await _context.Canteens.AnyAsync(c => c.Name.ToUpper() == request.Name.ToUpper());
            if (exists)
                throw new ValidationException($"Canteen with same name already exists");

            Canteen c = new Canteen(request.Name, request.Location, request.Capacity);
            c.WorkingHours = request.WorkingHours.Select(wh => new WorkingHour
            {
                Meal = Enum.Parse<Meal>(wh.Meal, ignoreCase: true),
                StartTime = TimeOnly.Parse(wh.From),
                EndTime = TimeOnly.Parse(wh.To)
            }).ToList();

            await _context.Canteens.AddAsync(c);
            await _context.SaveChangesAsync();

            return new CanteenResponse
            {
                Id = c.CanteenId.ToString(),
                Name = c.Name,
                Location = c.Location,
                Capacity = c.Capacity,
                WorkingHours = c.WorkingHours.Select(wh => new WorkingHourDto
                {
                    Meal = wh.Meal.ToString(),
                    From = wh.StartTime.ToString("HH:mm"),
                    To = wh.EndTime.ToString("HH:mm")
                }).ToList()

            };
        }

        public async Task<CanteenResponse> GetByIdAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;

            Canteen? c = await _context.Canteens.FindAsync(guid);
            if (c is null)
                return null;
            else return new CanteenResponse
            {
                Id = c.CanteenId.ToString(),
                Name = c.Name,
                Location = c.Location,
                Capacity = c.Capacity,
                WorkingHours = c.WorkingHours.Select(wh => new WorkingHourDto
                {
                    Meal = wh.Meal.ToString(),
                    From = wh.StartTime.ToString("HH:mm"),
                    To = wh.EndTime.ToString("HH:mm")
                }).ToList()
            };


        }

        public async Task<CanteensResponse> GetCanteensAsync()
        {
            var canteens = await _context.Canteens.Include(c => c.WorkingHours).ToListAsync();
            CanteensResponse result = new CanteensResponse();
            foreach (var c in canteens)
            {
                result.Canteens.Add(new CanteenResponse
                {
                    Id = c.CanteenId.ToString(),
                    Name = c.Name,
                    Location = c.Location,
                    Capacity = c.Capacity,
                    WorkingHours = c.WorkingHours.Select(wh => new WorkingHourDto
                    {
                        Meal = wh.Meal.ToString(),
                        From = wh.StartTime.ToString("HH:mm"),
                        To = wh.EndTime.ToString("HH:mm")

                    }).ToList()

                });
            }

            return result;
        }

        public async Task<CanteenResponse?> UpdateAsync(string id, UpdateCanteenRequest request)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;

            var canteen = await _context.Canteens.Include(c => c.WorkingHours).FirstOrDefaultAsync(c => c.CanteenId == guid);
            if (canteen is null)
                return null;

            if (request.Name is not null)
                canteen.Name = request.Name;
            if (request.Location is not null)
                canteen.Location = request.Location;
            if (request.Capacity is not null)
                canteen.Capacity = request.Capacity.Value;
            if (request.WorkingHours is not null)
                canteen.WorkingHours = request.WorkingHours.Select(wh => new WorkingHour
                {
                    Meal = Enum.Parse<Meal>(wh.Meal, true),
                    StartTime = TimeOnly.Parse(wh.From),
                    EndTime = TimeOnly.Parse(wh.To)
                }).ToList();

            await _context.SaveChangesAsync();

            return new CanteenResponse
            {
                Id = canteen.CanteenId.ToString(),
                Name = canteen.Name,
                Location = canteen.Location,
                Capacity = canteen.Capacity,
                WorkingHours = canteen.WorkingHours.Select(wh => new WorkingHourDto
                {
                    Meal = wh.Meal.ToString(),
                    From = wh.StartTime.ToString("HH:mm"),
                    To = wh.EndTime.ToString("HH:mm")
                }).ToList()
            };
        }


        public async Task<bool> DeleteAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return false;

            var canteen = await _context.Canteens
                .Include(c => c.WorkingHours)
                .FirstOrDefaultAsync(c => c.CanteenId == guid);
            if (canteen is null)
                return false;

            _context.Canteens.Remove(canteen);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<CanteenStatus?> GetRemainingCapacityAsync(
            string canteenId,
            DateOnly startDate,
            DateOnly endDate,
            TimeOnly startTime,
            TimeOnly endTime,
            uint duration)
        {
            var canteen = await _context.Canteens
                .Include(c => c.WorkingHours)
                .FirstOrDefaultAsync(c => c.CanteenId.ToString() == canteenId);

            if (canteen == null)
                return null;

            var slots = new List<CanteenSlot>();

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                foreach (var workingHour in canteen.WorkingHours)
                {
                    //Find intersection of fixed working hour and query interval
                    var intervalStart = startTime > workingHour.StartTime ? startTime : workingHour.StartTime;
                    var intervalEnd = endTime < workingHour.EndTime ? endTime : workingHour.EndTime;

                    if (intervalStart >= intervalEnd)
                        continue; // No overlap

                    // Split intersection interval into meal slots
                    for (var slotStart = intervalStart; slotStart < intervalEnd; slotStart = slotStart.Add(TimeSpan.FromMinutes(duration)))
                    {
                        var slotEnd = slotStart.Add(TimeSpan.FromMinutes(duration));
                        if (slotEnd > intervalEnd)
                            slotEnd = intervalEnd;

                        // Calculate reserved seats overlapping this slot
                        var reservedSeats = await _context.Reservations
                                .Where(r => r.CanteenId == canteen.CanteenId &&
                                r.Date == date &&
                                r.Time < slotEnd &&
                                r.Time.AddMinutes(r.Duration) > slotStart &&
                                r.Status == ReservationStatus.ACTIVE)
                                .CountAsync();



                        var remainingCapacity = Math.Max(0, (int)canteen.Capacity - reservedSeats);


                        slots.Add(new CanteenSlot
                        {
                            Date = date.ToString("yyyy-MM-dd"),
                            Meal = workingHour.Meal.ToString(),
                            StartTime = slotStart.ToString("HH:mm"),
                            RemainingCapacity = (uint)remainingCapacity
                        });
                    }
                }
            }

            return new CanteenStatus
            {
                CanteenId = canteenId,
                Slots = slots
            };

        }
    }

}