using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using BusinessSearch.Data;
using Microsoft.AspNetCore.Identity;
using BusinessSearch.Models;
using BusinessSearch.Services;
using BusinessSearch.Services.WebsiteOpportunitiesServices;
using BusinessSearch.Services.WebsiteOpportunitiesServices.Interfaces;
using BusinessSearch.Services.Email;
using Microsoft.AspNetCore.Identity.UI.Services;
using System;

namespace BusinessSearch
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        private readonly IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            _environment = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC services
            services.AddControllersWithViews();
            services.AddRazorPages();

            // Configure DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                        sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                    });
                if (_environment.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });

            // Configure Identity
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            //Email Config
            // Configure Email
            services.Configure<SendGridSettings>(Configuration.GetSection("SendGridSettings"));
            services.AddScoped<IEmailSender, SendGridEmailService>();
            services.AddScoped<IEmailService, SendGridEmailService>();

            // Configure Cookie Policy
            services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.Cookie.Name = "BusinessSearch";
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.SlidingExpiration = true;
            });

            // Register Core Services
            services.AddHttpClient();
            services.AddMemoryCache();
            services.AddHttpContextAccessor();

            // Register Organization Services
            services.AddScoped<IOrganizationFilterService, OrganizationFilterService>();
            services.AddScoped<IOrganizationService, OrganizationService>();
            services.AddScoped<ISearchUsageService, SearchUsageService>();

            // Register Website Analysis Services
            services.AddScoped<IWebsiteAnalysisService, WebsiteAnalysisService>();
            services.AddScoped<IGdprComplianceService, GdprComplianceService>();
            services.AddScoped<IPageSpeedService, PageSpeedService>();
            services.AddScoped<IResponsivenessService, ResponsivenessService>();
            services.AddScoped<ILocalSeoService, LocalSeoService>();
            services.AddScoped<IAccessibilityService, AccessibilityService>();
            services.AddScoped<IWebsiteOpportunitiesService, WebsiteOpportunitiesService>();
            services.AddScoped<BusinessDataService>();

            //Scripts and Templates Services
            services.AddScoped<TemplatesScriptsService>();

            // Register Business Services
            services.AddScoped<TeamService>();
            services.AddScoped<CrmService>();
            services.AddScoped<SearchHistoryService>();
            services.AddScoped<IOrganizationInviteService, OrganizationInviteService>();

            // Register Background Services
            services.AddHostedService<SearchUsageService>();

            // ImportExport Services
            services.AddScoped<IImportExportService, ImportExportService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}