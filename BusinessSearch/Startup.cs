using BusinessSearch.Data;
using BusinessSearch.Services;
using BusinessSearch.Services.WebsiteOpportunitiesServices;
using BusinessSearch.Services.WebsiteOpportunitiesServices.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BusinessSearch
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Configure DbContext with SQL Server
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection"),
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 10,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null);
                    }));

            services.AddControllersWithViews();

            // Register base services
            services.AddScoped<BusinessDataService>();
            services.AddScoped<CrmService>();
            services.AddScoped<ISearchHistoryService, SearchHistoryService>(); // Add SearchHistoryService

            // Register all analysis services in the correct order
            services.AddScoped<IResponsivenessService, ResponsivenessService>();
            services.AddScoped<IGdprComplianceService, GdprComplianceService>();
            services.AddScoped<IPageSpeedService, PageSpeedService>();
            services.AddScoped<IAccessibilityService, AccessibilityService>();
            services.AddScoped<ILocalSeoService, LocalSeoService>();
            services.AddScoped<IWebsiteOpportunitiesService, WebsiteOpportunitiesService>();

            // Configure HTTP clients
            services.AddHttpClient();
            services.AddHttpClient<IAccessibilityService, AccessibilityService>();

            // Register ILogger explicitly (although AddLogging should handle this)
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });

            // Add Memory Cache
            services.AddMemoryCache();

            // Configure Session
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Add Antiforgery
            services.AddAntiforgery(options =>
            {
                options.HeaderName = "RequestVerificationToken";
            });

            // Configure CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    builder =>
                    {
                        builder
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ApplicationDbContext context)
        {
            // Ensure database is created
            context.Database.EnsureCreated();

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
            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSession();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}