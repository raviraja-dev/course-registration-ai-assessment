using CourseRegistration.Application.Interfaces;
using CourseRegistration.Application.Services;
using CourseRegistration.Infrastructure.SampleData;
using CourseRegistration.Infrastructure.Repositories;


var builder = WebApplication.CreateBuilder(args);
// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSingleton<ICourseRepository>(
    new InMemoryCourseRepository(
        Data.CreateInitialState()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();


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
