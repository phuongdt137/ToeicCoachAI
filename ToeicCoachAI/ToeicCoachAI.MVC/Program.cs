using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using ToeicCoachAI.Repository.Data;
using ToeicCoachAI.Repository.Interfaces;
using ToeicCoachAI.Repository.Repositories;
using ToeicCoachAI.Service.BackgroundJobs;
using ToeicCoachAI.Service.Interfaces;
using ToeicCoachAI.Service.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 1. Add DbContext (SQL Server)
builder.Services.AddDbContext<ToeicCoachAiDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlOptions => sqlOptions.MigrationsAssembly("ToeicCoachAI.Repository")));

// 2. Register Core Repositories & Unit of Work
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// 3. Register Business Services & Background Jobs
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAiQuestionPipelineJob, AiQuestionPipelineJob>();

// 4. Configure Cookie & Google OAuth Authentication
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/Login";
    options.Cookie.Name = "ToeicCoachAI.Auth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/signin-google";
        options.SaveTokens = true;
    });
}

// 5. Configure Hangfire (SQL Server Storage + Background Server)
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true,
        PrepareSchemaIfNecessary = true
    }));

builder.Services.AddHangfireServer();

// 6. Add MVC Controllers with Views
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Seed Initial Database Curriculum & Rules
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ToeicCoachAiDbContext>();
    await DbInitializer.SeedAsync(dbContext);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard (/hangfire) & Scheduled Cron Job (02:00 AM daily)
app.UseHangfireDashboard("/hangfire");
RecurringJob.AddOrUpdate<IAiQuestionPipelineJob>(
    "auto-refill-question-pool",
    job => job.ExecuteAutoRefillAsync(CancellationToken.None),
    "0 2 * * *");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
