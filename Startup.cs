using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EmployeeManagement.Models;
using EmployeeManagement.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement
{
    public class Startup
    {
        private IConfiguration _config;
        

        public Startup(IConfiguration config)
        {
            _config= config;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            //DB set
            services.AddDbContextPool<AppDbContext>(
            options => options.UseSqlServer(_config.GetConnectionString("EmployeeDBConnection")));

            //Change the AcccessDenied path URL
            services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = new PathString("/Administration/AccessDenied");
            });

            //ADD the identity services
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
               {
                   options.Password.RequiredLength = 10;  // overrides the validation rules
                   options.Password.RequiredUniqueChars =3;

                   options.SignIn.RequireConfirmedEmail = true;

                   // for the account logout
                   options.Lockout.MaxFailedAccessAttempts = 5;
                   options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

               } )
               .AddEntityFrameworkStores<AppDbContext>()
               .AddDefaultTokenProviders(); 


            // AddMvc internally call AddMvcCore
            // services.AddMvcCore();
          //  services.AddMvc().AddXmlSerializerFormatters();
            services.AddMvc(config => {
                // set the authorize globally
                var policy = new AuthorizationPolicyBuilder()
                                .RequireAuthenticatedUser()
                                .Build();
                config.Filters.Add(new AuthorizeFilter(policy));
            }).AddXmlSerializerFormatters();

            //Register google authentication
            services.AddAuthentication().AddGoogle(options =>
            {
                options.ClientId = "882909025215-sdpl11ar8kjvjvacasupc82503e838jo.apps.googleusercontent.com";
                options.ClientSecret = "QU_tg6POcS5lhphoiN-6cJ3H";
                //options.CallbackPath = "";

            }).AddFacebook(options=>
            {
                options.AppId = "2414563498672260";
                options.AppSecret = "09cb332b38c5aff3ef3747d60c5b28fb";
            }) ;

            //Register the claim policy
            services.AddAuthorization(options =>
            {
                options.AddPolicy("DeleteRolePolicy",
                    policy => policy.RequireClaim("Delete Role"));

                //options.AddPolicy("EditRolePolicy", 
                //    policy => policy.RequireClaim("Edit Role" ,"true"));

                //options.AddPolicy("EditRolePolicy", policy => policy.RequireAssertion(context =>
                //   context.User.IsInRole("Admin") &&
                //   context.User.HasClaim(claim => claim.Type == "Edit Role" && claim.Value == "true") ||
                //   context.User.IsInRole("Super Admin")
                //));

                options.AddPolicy("EditRolePolicy", policy =>
                    policy.AddRequirements(new ManageAdminRolesAndClaimsRequirement()));
            });

            

            //Register the server
            services.AddScoped<IEmployeeRepository, SQLEmployeeRepository>();

            //Register the requirement handler policy
            services.AddSingleton<IAuthorizationHandler,
                            CanEditOnlyOtherAdminRolesAndClaimsHandler>();
            //Register the requirement handler 2 policy
            services.AddSingleton<IAuthorizationHandler,
                           SuperAdminHandler>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env  )
        {
            if (env.IsDevelopment())
            {
                //DeveloperExceptionPageOptions developerExceptionPageOptions = new DeveloperExceptionPageOptions();
                //developerExceptionPageOptions.SourceCodeLineCount = 1;
                //app.UseDeveloperExceptionPage(developerExceptionPageOptions);

                app.UseDeveloperExceptionPage();
            }
            else{
                app.UseExceptionHandler("/Error");  // Global Exception Hnadling
                app.UseStatusCodePagesWithRedirects("/Error/{0}");
            }

            //  DefaultFilesOptions defaultFilesOptions = new DefaultFilesOptions();
            //defaultFilesOptions.DefaultFileNames.Clear();
            //defaultFilesOptions.DefaultFileNames.Add("foo.html");
            //app.UseDefaultFiles(defaultFilesOptions);
            //app.UseStaticFiles();
            // app.UseFileServer();

            //FileServerOptions fileServerOptions = new FileServerOptions();
            // fileServerOptions.DefaultFilesOptions.DefaultFileNames.Clear();
            // fileServerOptions.DefaultFilesOptions.DefaultFileNames.Add("foo.html");
            // app.UseFileServer(fileServerOptions);

            //app.UseFileServer();
            app.UseStaticFiles();

            // should add before UseMvc mtd bcz only request(access) the auth user
            app.UseAuthentication();
            
            //app.UseMvcWithDefaultRoute(); Index is default

            // Conventional Routing //////////
            app.UseMvc(routes =>
            {
                routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });

            // Atribute Routing //////////
            // app.UseMvc();



            //app.Run(async (context) =>
            //{
            //    // await context.Response.WriteAsync(_config["MyKey"]);
            //    //await context.Response.WriteAsync(System.Diagnostics.Process.GetCurrentProcess().ProcessName);
            //    // throw new Exception("Some errors are processing the request ");
            //    //  await context.Response.WriteAsync("Hosting Envirnment "+env.EnvironmentName);
            //    //  logger.LogInformation("MW1 :Incoming Request ");
            //    //  await next();
            //    // logger.LogInformation("MW1 :Outgoing Response ");
            //    await context.Response.WriteAsync("Hello World");

            //});

        }
    }
}
