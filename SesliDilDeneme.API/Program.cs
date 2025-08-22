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
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Mvc;                         
using SesliDilDeneme.API.Filters;                  
using SesliDilDeneme.API.Middlewares;                    


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
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<TtsService>();
builder.Services.AddHostedService<AudioCleanupService>();
builder.Services.AddHostedService<CleanupService>();
builder.Services.AddSingleton<AgentActivityService>();
builder.Services.AddScoped<IUserDailyActivityService, UserDailyActivityService>();
builder.Services.AddScoped<IAgentActivityStatsService, AgentActivityStatsService>();
builder.Services.AddScoped<PromptService>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// SignalR
builder.Services.AddSignalR();

// HTTP Client
builder.Services.AddHttpClient();

// FluentValidation
builder.Services.AddControllers().AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<UserValidator>());
builder.Services.AddControllers().AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<ProgressValidator>());
builder.Services.AddControllers().AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<MessageValidator>());
builder.Services.AddControllers().AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<ConversationValidator>());
builder.Services.AddControllers().AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<AIAgentValidator>());
builder.Services.AddControllers().AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<SendMessageRequestValidator>());


builder.Services.Configure<ApiBehaviorOptions>(o =>
{
    // ModelState hatalarını default 400'e bırakma; biz filter ile yöneteceğiz
    o.SuppressModelStateInvalidFilter = true;
});

builder.Services.Configure<MvcOptions>(o =>
{
    // Global filter'lar
    o.Filters.Add<ApiResponseValidationFilter>(); // ModelState -> 400 tek tip
    o.Filters.Add<ResponseWrappingFilter>();      // Başarıları ApiResponse<T>.Ok ile sar
});

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
e:


builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .WithOrigins("http://167.172.162.242:5000", "http://localhost") 
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});
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
var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
if (!Directory.Exists(wwwrootPath))
{
    Directory.CreateDirectory(wwwrootPath);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(wwwrootPath),
    RequestPath = ""
});
app.UseRouting();
app.UseCors("AllowAll");

app.UseMiddleware<ExceptionHandlingMiddleware>();          


app.UseAuthentication(); // ÖNCE Authentication
app.UseAuthorization();  // SONRA Authorization
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHub<ChatHub>("/chatHub");
});


app.Run();
