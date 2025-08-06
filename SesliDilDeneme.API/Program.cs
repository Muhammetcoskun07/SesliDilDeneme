using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SesliDil.Data.Context;
using SesliDil.Core.Interfaces;
using SesliDil.Data.Repositories;
using SesliDil.Core.Mappings;
using SesliDil.Service.Services;
using SesliDil.Core.Entities;
using SesliDil.Service.Interfaces;
using FluentValidation.AspNetCore;
using SesliDilDeneme.API.Validators;
using SesliDilDeneme.API.Hubs;
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

// DbContext
builder.Services.AddDbContext<SesliDilDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories ve Service'ler
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped(typeof(IService<>), typeof(Service<>));

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ConversationService>();
builder.Services.AddScoped<MessageService>();
builder.Services.AddScoped<ProgressService>();
builder.Services.AddScoped<AIAgentService>();
builder.Services.AddScoped<FileStorageService>();
builder.Services.AddScoped<SessionService>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// SignalR
builder.Services.AddSignalR();

// HTTP Client
builder.Services.AddHttpClient();

// FluentValidation
builder.Services.AddControllers().AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<UserValidator>());
builder.Services.AddControllers().AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<ProgressValidator>());
builder.Services.AddControllers().AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<MessageValidator>());
builder.Services.AddControllers().AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<FileStorageValidator>());
builder.Services.AddControllers().AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<ConversationValidator>());
builder.Services.AddControllers().AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<AIAgentValidator>());
builder.Services.AddControllers().AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<SendMessageRequestValidator>());

// JWT Authentication
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
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

var app = builder.Build();

 if (app.Environment.IsDevelopment())
 {
     app.UseSwagger();
     app.UseSwaggerUI();
 }
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SesliDilDbContext>();
    db.Database.Migrate(); 
}
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication(); // ÖNCE Authentication
app.UseAuthorization();  // SONRA Authorization

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<ChatHub>("/chatHub");
});

app.Run();
