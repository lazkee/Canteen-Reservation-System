using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Application.Students;
using Infrastructure.Services;
using Application.Canteens;
using Application.Auth;

namespace CanteenReservationSystem;
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
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddSingleton<CanteenValidator>();

        builder.Services.AddScoped<ICanteenService, CanteenService>();
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

