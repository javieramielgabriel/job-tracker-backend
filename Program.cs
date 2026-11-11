using JobTracker.Api.Common.Constants;
using JobTracker.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddPersistenceServices(builder.Configuration)
    .AddApplicationServices()
    .AddApiPresentation(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseCors(ApiConstants.AngularCorsPolicy);
app.MapControllers();

app.Run();

public partial class Program;
