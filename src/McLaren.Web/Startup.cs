using System;
using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;
using McLaren.Core.Interfaces;
using McLaren.Core.Interfaces.Repositories;
using McLaren.Core.Services;
using McLaren.Infrastructure.Data;
using McLaren.Infrastructure.Data.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Versioning.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace McLaren.Web
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
            services.AddControllers(setupAction =>
            {
                setupAction.ReturnHttpNotAcceptable = true;
            })
            .AddJsonOptions(options =>
                options.JsonSerializerOptions.ReferenceHandling = ReferenceHandling.Preserve
            );
            
            if(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
            {
                services.AddDbContext<McLarenContext>(options =>
                        options.UseSqlServer(Configuration.GetConnectionString("AzureSQLConnection")));
            }
            else
            {
                services.AddDbContext<McLarenContext>(options =>
                    options.UseSqlite(Configuration.GetConnectionString("SQLiteConnection")));
            }

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

            services.AddScoped<IGrandPrixesRepository, GrandPrixesRepository>();
            services.AddScoped<IGrandPrixesService, GrandPrixesService>();

            services.AddScoped<ICarsRepository, CarsRepository>();
            services.AddScoped<ICarsService, CarsService>();

            services.AddScoped<IDriversRepository, DriversRepository>();
            services.AddScoped<IDriversService, DriversService>();

            services.AddSingleton<ILogService>((container) =>
            {
                var logger = container.GetRequiredService<ILogger<LogService>>();
                return new LogService() { Logger = logger };
            });        

            services.AddApiVersioning(
                options =>
                {
                    options.ReportApiVersions = true;

                    options.Conventions.Add(new VersionByNamespaceConvention());
                });

            services.AddVersionedApiExplorer(
                options =>
                {                    
                    options.GroupNameFormat = "'v'VVV";

                    options.SubstituteApiVersionInUrl = true;
                } );

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v0.9", new OpenApiInfo { Title = "McLaren API", Version = "v0.9" });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSwagger(c =>
            {
                c.RouteTemplate = "docs/{documentName}/docs.json";
            });

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/docs/v0.9/docs.json", "McLaren API V0.9");
                c.RoutePrefix = "docs";
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
