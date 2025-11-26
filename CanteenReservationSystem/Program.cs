using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
namespace CanteenReservationSystem;
using Application.Students;
using Infrastructure.Services;
public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        //In-memory db
        builder.Services.AddDbContext<CanteenDbContext>(options =>
        options.UseInMemoryDatabase("CanteenReservationDb"));

        builder.Services.AddScoped<IStudentService, StudentService>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();


        app.MapControllers();

        app.Run();
    }
}

