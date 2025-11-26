using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Application.Auth;
using Application.Canteens;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace CanteenReservationSystem.Controllers
{
    [Route("api/[controller]")]
    public class CanteensController : Controller
    {
        private readonly ICanteenService _canteenService;
        private readonly CanteenValidator _canteenValidator;
        private readonly IAuthService _authService;

        public CanteensController(ICanteenService canteenService, CanteenValidator canteenValidator, IAuthService authService)
        {
            _authService = authService;
            _canteenService = canteenService;
            _canteenValidator = canteenValidator;
        }

        [HttpPost]
        public async Task<IActionResult> CreateCanteen([FromHeader(Name = "studentId")] string studentId, [FromBody] CreateCanteenRequest request)
        {
            if (!await _authService.IsStudentAdminAsync(studentId))
                return StatusCode(403, "Only admins can add new canteens");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            IEnumerable<string> validationErrors = _canteenValidator.Validate(request);
            if (validationErrors.Any())
                return BadRequest(validationErrors);

            try
            {
                var result = await _canteenService.CreateAsync(request);
                return CreatedAtAction(nameof(GetCanteenById), new { id = result.Id }, result);
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred." });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCanteenById(string id)
        {
            var result = await _canteenService.GetByIdAsync(id);
            if (result is null)
                return NotFound();
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetCanteens()
        {
            var result = await _canteenService.GetCanteensAsync();

            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCanteen(
            [FromRoute] string id,
            [FromHeader(Name = "studentId")] string studentId,
            [FromBody] UpdateCanteenRequest request)
        {
            if (!await _authService.IsStudentAdminAsync(studentId))
                return StatusCode(403, "Only admins can update canteens");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            IEnumerable<string> validationErrors = _canteenValidator.Validate(request);
            if (validationErrors.Any())
                return BadRequest(validationErrors);

            var result = await _canteenService.UpdateAsync(id, request); 
            if (result is null)
                return NotFound();

            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCanteen([FromRoute] string id, [FromHeader(Name = "studentId")] string studentId)
        {
            if (!await _authService.IsStudentAdminAsync(studentId))
                return StatusCode(403, "Only admins can delete canteens");

            var deleted = await _canteenService.DeleteAsync(id);
            if (!deleted)
                return NotFound();

            return NoContent();
        }

        [HttpGet("{id}/status")]
        public async Task<IActionResult> GetCanteenStatus(
            string id,
            [FromQuery] DateOnly startDate,
            [FromQuery] DateOnly endDate,
            [FromQuery] TimeOnly startTime,
            [FromQuery] TimeOnly endTime,
            [FromQuery] uint duration)

        {
            var errors = _canteenValidator.ValidateStatusRequest(startDate, endDate, startTime,
                endTime, duration);

            if (errors.Any())
                return BadRequest(errors);

            var status = await _canteenService.GetRemainingCapacityAsync(id, startDate, endDate, startTime, endTime, duration);
            if (status == null)
                return NotFound();

            return Ok(status);
        }

    }
}

