using KingdomPrep.Web.Components;
using KingdomPrep.Web.Core;
using KingdomPrep.Web.Core.Auth;
using KingdomPrep.Web.Data;
using KingdomPrep.Web.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// --- Data ---
builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddDbContextFactory<AppDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Default")), ServiceLifetime.Scoped);
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IStudentStats, StudentStatsRepository>();
builder.Services.AddScoped<IDashboardStats, DashboardStatsRepository>();
builder.Services.AddScoped<IWardRepository, WardRepository>();
builder.Services.AddScoped<IWardBillingRepository, WardBillingRepository>();
builder.Services.AddScoped<ISchoolProfileService, SchoolProfileService>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IStudentRemarksRepository, StudentRemarksRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IStudentPortalRepository, StudentPortalRepository>();
builder.Services.AddScoped<IReportCardPortalRepository, ReportCardPortalRepository>();
builder.Services.AddScoped<IAttendancePortalRepository, AttendancePortalRepository>();
builder.Services.AddScoped<IStaffAttendanceService, StaffAttendanceService>();
builder.Services.AddScoped<ITeacherPortalRepository, TeacherPortalRepository>();
builder.Services.AddScoped<IGradingPortalRepository, GradingPortalRepository>();
builder.Services.AddScoped<IDefaulterPortalRepository, DefaulterPortalRepository>();
builder.Services.AddScoped<ILeavePortalRepository, LeavePortalRepository>();
builder.Services.AddScoped<IParentPortalRepository, ParentPortalRepository>();
builder.Services.AddScoped<IAcademicPortalRepository, AcademicPortalRepository>();
builder.Services.AddScoped<IAdmissionRepository, AdmissionRepository>();
builder.Services.AddScoped<IFinancePortalRepository, FinancePortalRepository>();
builder.Services.AddScoped<IPerformanceReportRepository, PerformanceReportRepository>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IUserAccountService, UserAccountService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IPaymentGatewaySettingsService, PaymentGatewaySettingsService>();
builder.Services.AddScoped<IOnlinePaymentIntentRepository, OnlinePaymentIntentRepository>();
builder.Services.AddScoped<IPortalTenantContext, PortalTenantContext>();
builder.Services.AddScoped<IHeadmasterTenantProvider, HeadmasterTenantProvider>();
builder.Services.AddScoped<IHeadmasterService, HeadmasterService>();
builder.Services.AddScoped<WebReportCardGenerator>();
builder.Services.AddHttpClient<IPaystackPaymentVerifier, PaystackPaymentVerifier>();
builder.Services.AddHttpClient<IMomoPaymentService, MomoPaymentService>();

// --- Auth ---
builder.Services.AddScoped<IUserLookup, UserLookupAdapter>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ILoginThrottle, LoginThrottle>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/login";
        o.AccessDeniedPath = "/denied";
        o.Cookie.HttpOnly = true;
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization(options =>
{
    foreach (var policyName in WebPermission.Names)
    {
        var name = policyName;
        options.AddPolicy(name, policy => policy.RequireAssertion(context =>
            WebPermission.HasPermission(context.User, name)));
    }
});
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Logout endpoint (CSRF-low; SameSite=Strict cookie + POST-only).
app.MapPost("/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).DisableAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
