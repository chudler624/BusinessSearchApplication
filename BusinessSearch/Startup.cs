using Microsoft.EntityFrameworkCore;
using BusinessSearch.Data;
using BusinessSearch.Services;
using BusinessSearch.Services.WebsiteOpportunitiesServices;
using System;

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

           // Register services
           services.AddScoped<BusinessDataService>();
           services.AddScoped<CrmService>();

           // Register WebsiteOpportunities services
           services.AddScoped<IResponsivenessService, ResponsivenessService>();
           services.AddScoped<IGdprComplianceService, GdprComplianceService>();
           services.AddScoped<IWebsiteOpportunitiesService, WebsiteOpportunitiesService>();

           // Configure HTTP client
           services.AddHttpClient();

           // Add Memory Cache
           services.AddMemoryCache();

           // Add session services
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

           // Add CORS if needed
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

           // Use CORS
           app.UseCors("AllowAll");

           app.UseAuthentication();
           app.UseAuthorization();

           // Use session
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