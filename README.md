# Canteen-Reservation-System

Project for the Challenge Phase of Levi9's 5 Days in the Cloud competition.  
Backend API for managing students, canteens, and reservations in student canteens.

## Technologies used

- .NET SDK 8.0.404
- ASP.NET Core Web API 8.0
- Entity Framework Core 8.0.11 (InMemory DB)
- xUnit 2.5.x
- FluentAssertions 8.8.0

## Environment setup

1. Install .NET SDK 8.0.
2. Clone the repository and go to the solution folder:

git clone <repository-url>
cd Canteen-Reservation-System

text

3. (Optional) Set the environment variable:

export ASPNETCORE_ENVIRONMENT=Development

text

## How to build

From the solution root:

dotnet build

text

This command builds the following projects: `Domain`, `Application`, `Infrastructure`, `CanteenReservationSystem`, `CanteenReservationSystem.Tests`.

## How to run the application

From the solution root:

dotnet run --project CanteenReservationSystem/CanteenReservationSystem.csproj

text

By default the API is available at:

- http://localhost:5270
- https://localhost:7024

Swagger UI is available at `/swagger` on these URLs.

## How to run unit tests

From the solution root:

dotnet test

text

This command runs all tests in the `CanteenReservationSystem.Tests` project  
(`CanteenServiceTests`, `StudentServiceTests`, `ReservationServiceTests`).
