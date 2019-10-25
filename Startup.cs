using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Mercer.Excel.Processor.WebUi.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mercer.Excel.Processor.WebUi.Identity;

namespace Mercer.Excel.Processor.WebUi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<AppUser, AppRole>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddRazorPages()
                .AddRazorRuntimeCompilation(); //add this nuget package because my markup changes were not being picked up in the browser.
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });

            CreateRoles(serviceProvider).Wait();
        }

        public async Task CreateRoles(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<AppRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

            var roleNames = Configuration.GetSection("UserSettings")["Roles"].Split(",");
            var userNames = Configuration.GetSection("UserSettings")["UserNames"].Split(",");
            var userEmails = Configuration.GetSection("UserSettings")["userEmails"].Split(",");
            var provisionalPassword = Configuration.GetSection("UserSettings")["Password"];

            IdentityResult roleResult;

            foreach(var roleName in roleNames)
            {
                var roleExists = await roleManager.RoleExistsAsync(roleName.Trim());
                if(!roleExists)
                {
                    roleResult = await roleManager.CreateAsync(new AppRole(roleName.Trim()));
                }
            }

            var space = " ";
            for (var i = 0; i < userNames.Length; i++)
            {
                var appUser = new AppUser
                {
                    Email = userEmails[i].Trim(),
                    FirstName = userNames[i].Split(space).First() ?? "tester",
                    LastName = userNames[i].Split(space).Last() ?? "tester",
                    UserName = userNames[i].Replace(" ", "-")
                };

                var user = await userManager.FindByEmailAsync(userEmails[i].Trim());
                if (user == null)
                {
                    var newUser = await userManager.CreateAsync(appUser, provisionalPassword);
                    if(newUser.Succeeded)
                    {
                        user = await userManager.FindByEmailAsync(userEmails[i].Trim());
                        foreach (var roleName in roleNames)
                        {
                            await userManager.AddToRoleAsync(user, roleName.Trim());
                        }
                    }
                }
            }
        }
    }
}
