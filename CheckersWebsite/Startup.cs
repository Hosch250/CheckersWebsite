using CheckersWebsite.Controllers;
using Database;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CheckersWebsite
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
            var connString = Configuration.GetConnectionString("Database");            

            // entity framework
            services.AddEntityFrameworkSqlServer()
                .AddDbContext<Context>(options =>
                    options.UseSqlServer(connString), ServiceLifetime.Transient
                );

            // Add MVC services to the services container.
            services.AddMvc();

            // new context on each request
            services.AddTransient<Context, Context>();
            services.AddMediatR();

            services.AddTransient<ComputerPlayer, ComputerPlayer>();

            services.AddSignalR();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseFileServer();

            app.UseWebSockets();
            app.UseSignalR(routes =>
            {
                routes.MapHub<SignalR.GameHub>("/gameHub");
                routes.MapHub<SignalR.APIHub>("/apiHub");
            });

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
