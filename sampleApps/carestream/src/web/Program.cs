using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using DbUp;
using carestream.web.Filters;
using carestream.core.interfaces.repositories;
using carestream.core.interfaces.services;
using carestream.core.services;
using carestream.persistence.repositories;
using CareStream.Persistence.DbUp;
using carestream.persistence.migrations;
using carestream.core.infrastructure;
using carestream.web.Middleware;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

var logtoOrigin = builder.Configuration["Logto:BaseURL"]
                  ?? throw new InvalidOperationException("Logto:BaseURL is not configured.");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                                    ? CookieSecurePolicy.SameAsRequest
                                    : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
})
.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["Logto:Authority"];
    options.ClientId = builder.Configuration["Logto:ClientId"];
    options.ClientSecret = builder.Configuration["Logto:ClientSecret"];

    if (builder.Environment.IsDevelopment())
    {
        options.RequireHttpsMetadata = false;
        Console.WriteLine($"[WARNING] Development mode: Allowing HTTP for OIDC Authority '{options.Authority}'. Ensure HTTPS is used in production.");
    }

    options.ResponseType = OpenIdConnectResponseType.Code;
    options.ResponseMode = OpenIdConnectResponseMode.Query;
    options.UsePkce = true;

    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("roles");

    options.MapInboundClaims = false;
    options.SaveTokens = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "name",
        RoleClaimType = "roles"
    };

    options.Events = new OpenIdConnectEvents
    {
        OnRedirectToIdentityProvider = context =>
        {
            if (builder.Environment.IsDevelopment())
            {
                if (!string.IsNullOrEmpty(logtoOrigin) && context.ProtocolMessage.IssuerAddress.Contains("logto:3001"))
                {
                    context.ProtocolMessage.IssuerAddress = context.ProtocolMessage.IssuerAddress.Replace("http://logto:3001", logtoOrigin);
                }
            }
            return Task.CompletedTask;
        },
        OnTicketReceived = async context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            var userRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
            var facilitySelectionService = context.HttpContext.RequestServices.GetRequiredService<IFacilitySelectionService>();

            var claimsIdentity = (ClaimsIdentity)context.Principal!.Identity!;
            var subClaim = claimsIdentity.FindFirst("sub");

            if (subClaim == null)
            {
                logger.LogError("OnTicketReceived: 'sub' claim missing for authenticated user.");
                context.Fail("Authentication failed: User ID missing.");
                return;
            }

            var internalUserId = await userRepository.GetUserIdByLogtoSubAsync(subClaim.Value);
            if (!internalUserId.HasValue)
            {
                logger.LogWarning("OnTicketReceived: User with Logto sub '{Sub}' not linked to an internal user. Denying access.", subClaim.Value);
                context.Fail("Access denied: User account not configured. Contact administrator.");
                return;
            }

            var accessibleFacilities = (await facilitySelectionService.GetFacilitiesForUserAsync(internalUserId.Value)).ToList();
            if (!accessibleFacilities.Any())
            {
                logger.LogWarning("OnTicketReceived: User {UserId} has no accessible facilities. Denying access.", internalUserId.Value);
                context.Fail("Access denied: User has no assigned facilities.");
                return;
            }

            var defaultFacility = await facilitySelectionService.GetDefaultFacilityForUserAsync(internalUserId.Value);
            if (defaultFacility == null)
            {
                logger.LogError("OnTicketReceived: Could not determine default facility for user {UserId}.", internalUserId.Value);
                context.Fail("System error: Could not determine user's default facility.");
                return;
            }

            // Remove old custom claims before adding new ones
            var oldCarestreamUserIdClaim = claimsIdentity.FindFirst("carestream_user_id");
            if (oldCarestreamUserIdClaim != null) claimsIdentity.RemoveClaim(oldCarestreamUserIdClaim);
            var oldUserFacilitiesClaim = claimsIdentity.FindFirst("carestream_user_facilities");
            if (oldUserFacilitiesClaim != null) claimsIdentity.RemoveClaim(oldUserFacilitiesClaim);
            var oldCurrentFacilityClaim = claimsIdentity.FindFirst("carestream_current_facility_id");
            if (oldCurrentFacilityClaim != null) claimsIdentity.RemoveClaim(oldCurrentFacilityClaim);


            // Add/re-add custom CareStream claims to the identity
            claimsIdentity.AddClaim(new Claim("carestream_user_id", internalUserId.Value.ToString(), ClaimValueTypes.Integer32));
            claimsIdentity.AddClaim(new Claim("carestream_user_facilities", JsonSerializer.Serialize(accessibleFacilities), ClaimValueTypes.String));
            claimsIdentity.AddClaim(new Claim("carestream_current_facility_id", defaultFacility.FacilityId.ToString(), ClaimValueTypes.Integer32));

            // Re-sign the authentication cookie with the updated principal and retain original properties
            await context.HttpContext.SignInAsync(context.Principal, context.Properties); // CORRECTED LINE

            logger.LogInformation("OnTicketReceived: Successfully added/updated CareStream claims and re-signed cookie for user {UserId} (Facility: {FacilityId}).", internalUserId.Value, defaultFacility.FacilityId);
        },
        OnRemoteFailure = context =>
        {
            context.HandleResponse();
            context.Response.Redirect("/Account/AccessDenied?message=" + Uri.EscapeDataString(context.Failure?.Message ?? "Authentication failed."));
            return Task.CompletedTask;
        },
        OnRedirectToIdentityProviderForSignOut = context =>
        {
            if (builder.Environment.IsDevelopment())
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var originalLogoutUrl = context.ProtocolMessage.IssuerAddress;

                logger.LogInformation("Original Logout URL: {Url}", originalLogoutUrl);

                if (!string.IsNullOrEmpty(logtoOrigin) && originalLogoutUrl.Contains("logto:3001"))
                {
                    var correctedUrl = originalLogoutUrl.Replace("http://logto:3001", logtoOrigin);
                    context.ProtocolMessage.IssuerAddress = correctedUrl;
                    logger.LogInformation("Corrected Logout URL for browser: {Url}", correctedUrl);
                }
            }
            return Task.CompletedTask;
        },
    };
});

// Register ICurrentFacilityContext as Scoped
builder.Services.AddScoped<ICurrentFacilityContext, CurrentFacilityContext>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<CheckHxRequestAttribute>();
});

// --- Register ALL Services ---
builder.Services.AddScoped<IPatientAdminDashboardService, PatientAdminDashboardService>();
builder.Services.AddScoped<IPatientService, PatientService>();
builder.Services.AddScoped<INurseDashboardService, NurseDashboardService>();
builder.Services.AddScoped<IVitalsService, VitalsService>();
builder.Services.AddScoped<IDoctorDashboardService, DoctorDashboardService>();
builder.Services.AddScoped<IPrescriptionService, PrescriptionService>();
builder.Services.AddScoped<ISickNoteService, SickNoteService>();
builder.Services.AddScoped<IPharmacyService, PharmacyService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();
builder.Services.AddScoped<IPatientAdminService, PatientAdminService>();
builder.Services.AddScoped<IEmergencyContactService, EmergencyContactService>();
builder.Services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
builder.Services.AddScoped<IFacilitySelectionService, FacilitySelectionService>();
builder.Services.AddScoped<IPatientQueueService, PatientQueueService>();
builder.Services.AddScoped<IFacilityAdminService, FacilityAdminService>();
builder.Services.AddScoped<IDepartmentAdminService, DepartmentAdminService>();
builder.Services.AddScoped<IWardAdminService, WardAdminService>();
builder.Services.AddScoped<IConsultationService, ConsultationService>();
builder.Services.AddScoped<IPatientHistoryService, PatientHistoryService>();
builder.Services.AddScoped<IVisitAssessmentService, VisitAssessmentService>();
builder.Services.AddScoped<IReferralService, ReferralService>();
builder.Services.AddScoped<IStaffReportService, StaffReportService>();
builder.Services.AddScoped<IMessagingService, MessagingService>();
builder.Services.AddScoped<IDD50ReportService, DD50ReportService>();
builder.Services.AddScoped<ISystemHealthService, SystemHealthService>();


// --- Register ALL Repositories ---
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IVisitRepository, VisitRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IVitalsRepository, VitalsRepository>();
builder.Services.AddScoped<IMedicationRepository, MedicationRepository>();
builder.Services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
builder.Services.AddScoped<ISickNoteRepository, SickNoteRepository>();
builder.Services.AddScoped<IDispensationRepository, DispensationRepository>();
builder.Services.AddScoped<IFacilityRepository, FacilityRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<IWardRepository, WardRepository>();
builder.Services.AddScoped<IIcd10CodeRepository, Icd10CodeRepository>();
builder.Services.AddScoped<IProcedureRepository, ProcedureRepository>();
builder.Services.AddScoped<IPatientMedicalHistoryRepository, PatientMedicalHistoryRepository>();
builder.Services.AddScoped<IVisitAssessmentRepository, VisitAssessmentRepository>();
builder.Services.AddScoped<IReferralRepository, ReferralRepository>();
builder.Services.AddScoped<IStaffReportRepository, StaffReportRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<ISystemHealthRepository, SystemHealthRepository>();


if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
    });

    builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.Fastest;
    });

    builder.Services.Configure<GzipCompressionProviderOptions>(options =>
    {
        options.Level = CompressionLevel.SmallestSize;
    });
}

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

EnsureDatabase.For.PostgresqlDatabase(connectionString);

var environment = builder.Environment;
var persistenceAssembly = typeof(PersistenceMarker).Assembly;

var scriptProvider = new ConditionalScriptProvider(persistenceAssembly, environment);

var upgrader = DeployChanges.To
    .PostgresqlDatabase(connectionString)
    .WithScripts(scriptProvider)
    .LogToConsole()
    .Build();

var result = upgrader.PerformUpgrade();

if (!result.Successful)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Database migration failed:");
    Console.WriteLine(result.Error);
    Console.ResetColor();
    throw new Exception("Database migration failed.", result.Error);
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Database migration successful!");
Console.ResetColor();

var app = builder.Build();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownProxies.Clear();
forwardedHeadersOptions.KnownNetworks.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseStaticFiles();
app.UseDefaultFiles();

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseRouting();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<FacilityContextMiddleware>();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();