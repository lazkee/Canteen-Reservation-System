using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Application.Canteens;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
            var exists = await _context.Canteens
                .AnyAsync(c => c.Name.ToUpper() == request.Name.ToUpper());

            if (exists)
                throw new ValidationException("Canteen with same name already exists");

            var canteen = new Canteen(request.Name, request.Location, request.Capacity)
            {
                WorkingHours = request.WorkingHours.Select(wh => new WorkingHour
                {
                    Meal = Enum.Parse<Meal>(wh.Meal, ignoreCase: true),
                    StartTime = TimeOnly.Parse(wh.From),
                    EndTime = TimeOnly.Parse(wh.To)
                }).ToList()
            };

            await _context.Canteens.AddAsync(canteen);
            await _context.SaveChangesAsync();

            return MapToResponse(canteen);
        }

        public async Task<CanteenResponse?> GetByIdAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;

            var canteen = await _context.Canteens
                .Include(c => c.WorkingHours)
                .FirstOrDefaultAsync(c => c.CanteenId == guid);

            if (canteen is null)
                return null;

            return MapToResponse(canteen);
        }

        public async Task<List<CanteenResponse>> GetCanteensAsync()
        {
            var canteens = await _context.Canteens
                .Include(c => c.WorkingHours)
                .ToListAsync();

            return canteens
                .Select(MapToResponse)
                .ToList();
        }


        public async Task<CanteenResponse?> UpdateAsync(string id, UpdateCanteenRequest request)
        {
            if (!Guid.TryParse(id, out var guid))
                return null;

            var canteen = await _context.Canteens
                .Include(c => c.WorkingHours)
                .FirstOrDefaultAsync(c => c.CanteenId == guid);

            if (canteen is null)
                return null;

            if (request.Name is not null)
                canteen.Name = request.Name;

            if (request.Location is not null)
                canteen.Location = request.Location;

            if (request.Capacity is not null)
                canteen.Capacity = request.Capacity.Value;

            if (request.WorkingHours is not null)
            {
                canteen.WorkingHours = request.WorkingHours.Select(wh => new WorkingHour
                {
                    Meal = Enum.Parse<Meal>(wh.Meal, ignoreCase: true),
                    StartTime = TimeOnly.Parse(wh.From),
                    EndTime = TimeOnly.Parse(wh.To)
                }).ToList();
            }

            await _context.SaveChangesAsync();

            return MapToResponse(canteen);
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

            var reservations = await _context.Reservations
                .Where(r => r.CanteenId == guid &&
                    r.Status != ReservationStatus.Cancelled)
                    .ToListAsync();

            foreach (var r in reservations)
            {
                r.Status = ReservationStatus.Cancelled;
            }
            _context.Canteens.Remove(canteen);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<CanteenStatus>> GetRemainingCapacityAsync(
                DateOnly startDate,
                DateOnly endDate,
                TimeOnly startTime,
                TimeOnly endTime,
                uint duration)
        {
            if (duration == 0)
                throw new ArgumentException("Duration must be greater than 0.", nameof(duration));

            var canteens = await _context.Canteens
                .Include(c => c.WorkingHours)
                .ToListAsync();

            var results = new List<CanteenStatus>();

            foreach (var canteen in canteens)
            {
                var slots = new List<CanteenSlot>();
                var slotDuration = TimeSpan.FromMinutes(duration);

                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    foreach (var workingHour in canteen.WorkingHours)
                    {
                        var intervalStart = startTime > workingHour.StartTime ? startTime : workingHour.StartTime;
                        var intervalEnd = endTime < workingHour.EndTime ? endTime : workingHour.EndTime;

                        if (intervalStart >= intervalEnd)
                            continue;

                        for (var slotStart = intervalStart; slotStart < intervalEnd; slotStart = slotStart.Add(slotDuration))
                        {
                            var slotEnd = slotStart.Add(slotDuration);
                            if (slotEnd > intervalEnd)
                                slotEnd = intervalEnd;

                            var reservedSeats = await GetReservedSeatsAsync(
                                canteen.CanteenId,
                                date,
                                slotStart,
                                slotEnd);

                            var remainingCapacity = Math.Max(0, (int)canteen.Capacity - reservedSeats);

                            slots.Add(new CanteenSlot
                            {
                                Date = date.ToString("yyyy-MM-dd"),
                                Meal = workingHour.Meal.ToString().ToLower(),
                                StartTime = slotStart.ToString("HH:mm"),
                                RemainingCapacity = (uint)remainingCapacity
                            });
                        }
                    }
                }

                results.Add(new CanteenStatus
                {
                    CanteenId = canteen.CanteenId.ToString(),
                    Slots = slots
                });
            }

            return results;
        }

        public async Task<CanteenStatus?> GetRemainingCapacityForCanteenAsync(
                string canteenId,
                DateOnly startDate,
                DateOnly endDate,
                TimeOnly startTime,
                TimeOnly endTime,
                uint duration)
        {
            if (duration == 0)
                throw new ArgumentException("Duration must be greater than 0.", nameof(duration));

            var canteen = await _context.Canteens
                .Include(c => c.WorkingHours)
                .FirstOrDefaultAsync(c => c.CanteenId.ToString() == canteenId);

            if (canteen is null)
                return null;

            var slots = new List<CanteenSlot>();
            var slotDuration = TimeSpan.FromMinutes(duration);

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                foreach (var workingHour in canteen.WorkingHours)
                {
                    var intervalStart = startTime > workingHour.StartTime ? startTime : workingHour.StartTime;
                    var intervalEnd = endTime < workingHour.EndTime ? endTime : workingHour.EndTime;

                    if (intervalStart >= intervalEnd)
                        continue;

                    for (var slotStart = intervalStart; slotStart < intervalEnd; slotStart = slotStart.Add(slotDuration))
                    {
                        var slotEnd = slotStart.Add(slotDuration);
                        if (slotEnd > intervalEnd)
                            slotEnd = intervalEnd;

                        var reservedSeats = await GetReservedSeatsAsync(
                            canteen.CanteenId,
                            date,
                            slotStart,
                            slotEnd);

                        var remainingCapacity = Math.Max(0, (int)canteen.Capacity - reservedSeats);

                        slots.Add(new CanteenSlot
                        {
                            Date = date.ToString("yyyy-MM-dd"),
                            Meal = workingHour.Meal.ToString().ToLower(),
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



        private static CanteenResponse MapToResponse(Canteen c) =>
            new CanteenResponse
            {
                Id = c.CanteenId.ToString(),
                Name = c.Name,
                Location = c.Location,
                Capacity = c.Capacity,
                WorkingHours = c.WorkingHours.Select(wh => new WorkingHourDto
                {
                    Meal = wh.Meal.ToString().ToLower(),
                    From = wh.StartTime.ToString("HH:mm"),
                    To = wh.EndTime.ToString("HH:mm")
                }).ToList()
            };

        private async Task<int> GetReservedSeatsAsync(
            Guid canteenGuid,
            DateOnly date,
            TimeOnly slotStart,
            TimeOnly slotEnd)
        {
            return await _context.Reservations
                .Where(r => r.CanteenId == canteenGuid &&
                            r.Date == date &&
                            r.Time < slotEnd &&
                            r.Time.AddMinutes(r.Duration) > slotStart &&
                            r.Status == ReservationStatus.Active)
                .CountAsync();
        }
    }
}
