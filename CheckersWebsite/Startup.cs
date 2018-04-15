using Database;
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
            string conString = ConfigurationExtensions
                  .GetConnectionString(Configuration, "Database");

            // entity framework
            services.AddEntityFrameworkSqlServer()
                .AddDbContext<Context>(options =>
                    options.UseSqlServer(conString)
                );

            // Add MVC services to the services container.
            services.AddMvc();

            // new context on each request
            services.AddScoped<Context, Context>();

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
                routes.MapHub<SignalR.MovesHub>("movesHub");
                routes.MapHub<SignalR.OpponentsHub>("opponentsHub");
            });

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapRoute(
                    name: "Board",
                    template: "{controller=Board}/{action}");
            });
        }
    }
}
