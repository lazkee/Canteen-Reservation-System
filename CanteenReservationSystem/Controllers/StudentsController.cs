using Infrastructure.Data;
using Application.Students;
using Domain.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace CanteenReservationSystem.Controllers
{
    [ApiController]
    [Route("students")]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;

        public StudentsController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        // POST /students
        [HttpPost]
        public async Task<IActionResult> CreateStudent([FromBody] CreateStudentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _studentService.CreateAsync(request);
                return CreatedAtAction(
                    nameof(GetStudentById),
                    new { id = result.Id },
                    result
                );
            }
            catch (ValidationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudentById(string id)
        {
            var result = await _studentService.GetByIdAsync(id);
            if (result is null)
                return NotFound();

            return Ok(result);
        }



    }
}
