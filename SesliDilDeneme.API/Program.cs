using Microsoft.EntityFrameworkCore;
using SesliDil.Data.Context;
using SesliDil.Core.Interfaces;
using SesliDil.Data.Repositories;
using SesliDil.Core.Mappings;
using SesliDil.Service.Interfaces;
using SesliDil.Service.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<SesliDilDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repository (Generic)
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Service (Generic)
builder.Services.AddScoped(typeof(IService<>), typeof(Service<>));
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ConversationService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<ProgressService>();
builder.Services.AddScoped<AIAgentService>();
builder.Services.AddScoped<FileStorageService>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });
builder.Services.AddAuthorization();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
