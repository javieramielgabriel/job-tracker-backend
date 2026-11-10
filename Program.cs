using System.Text.Json.Serialization;
using JobTracker.Api.Data;
using JobTracker.Api.Endpoints;
using JobTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("JobTrackerDb")
    ?? throw new InvalidOperationException("The JobTrackerDb connection string is not configured.");

builder.Services.AddDbContext<JobTrackerDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularFrontend", policy =>
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter<ApplicationStatus>(allowIntegerValues: false)));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors("AngularFrontend");
app.MapJobEndpoints();

app.Run();
