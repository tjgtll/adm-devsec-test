using App;
using App.Models;
using App.Services;
using App.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Yuniql.AspNetCore;
using Yuniql.Core;
using Yuniql.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ApiBehaviorOptions>(o =>
                {
                    o.SuppressModelStateInvalidFilter = true;
                });

builder.Services.AddControllers()
                .AddJsonOptions(o =>
                {
                    o.JsonSerializerOptions.WriteIndented = true;
                    o.JsonSerializerOptions.PropertyNamingPolicy = null;
                });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("Default")!;
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));

builder.Services.AddScoped<IValidator<ProcessRequest>, ProcessRequestValidator>();
builder.Services.AddScoped<IProcessService, ProcessService>();

var app = builder.Build();
var traceService = new ConsoleTraceService { IsDebugEnabled = true };

app.UseYuniql(new PostgreSqlDataService(traceService),
              new PostgreSqlBulkImportService(traceService),
              traceService, 
              new Yuniql.AspNetCore.Configuration
              {
                  Platform = SUPPORTED_DATABASES.POSTGRESQL,
                  Workspace = Path.Combine(AppContext.BaseDirectory, "Migrations"),
                  ConnectionString = app.Configuration.GetConnectionString("Default")!,
                  IsAutoCreateDatabase = true,
                  IsDebug = true
              });

app.UseSwagger(o => o.RouteTemplate = "api/swagger/{documentName}/swagger.json");

app.UseSwaggerUI(o => o.RoutePrefix = "api/swagger");

app.MapControllers();

app.Run();