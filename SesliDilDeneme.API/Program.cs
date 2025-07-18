using Microsoft.EntityFrameworkCore;
using SesliDil.Data.Context;
using SesliDil.Core.Interfaces;
using SesliDil.Data.Repositories;
using SesliDil.Core.Mappings;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<SesliDilDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository (Generic)
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
