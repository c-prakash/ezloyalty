using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Serilog;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Abstractions;
using ezLoyalty.Services.Actions.API.Application.IntegrationEvents.EventHandling;
using ezLoyalty.Services.Actions.API.Application.IntegrationEvents.Events;
using ezLoyalty.Services.Actions.API.Extensions;
using ezLoyalty.Services.Actions.API.Infrastructure.AutoFacModules;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.ApplicationInsights.Extensibility;

namespace ezLoyalty.Services.Actions.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddAppInsights(Configuration);

            services.AddControllers();
            //services.AddDbContext<ActionsDbContext>(options => options.UseInMemoryDatabase(databaseName: "ActionsInstance"));

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            services
              .AddCustomDbContext(Configuration)
              .AddIntegrationServices(Configuration)
              .AddEventBus(Configuration)
              .AddSwagger()
              .AddHealthChecks(Configuration);

            services.AddSingleton<ITelemetryInitializer>(new RoleNameInitializer(Program.AppName));

            var container = new ContainerBuilder();
            container.Populate(services);

            container.RegisterModule(new MediatorModule());
            container.RegisterModule(new ApplicationModule());

            return new AutofacServiceProvider(container.Build());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            var pathBase = Configuration["PATH_BASE"];
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseSerilogRequestLogging();

            app.UseSwagger()
             .UseSwaggerUI(c =>
             {
                 c.SwaggerEndpoint($"{ (!string.IsNullOrEmpty(pathBase) ? pathBase : string.Empty) }/swagger/v1/swagger.json", "ActionsAPI V1");
             });

            app.UseForwardedHeaders();
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                endpoints.MapHealthChecks("/hc", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    //ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapHealthChecks("/liveness", new HealthCheckOptions
                {
                    Predicate = r => r.Name.Contains("self")
                });
            });

            ConfigureEventBus(app);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        protected virtual void ConfigureEventBus(IApplicationBuilder app)
        {
            var eventBus = app.ApplicationServices.GetRequiredService<IEventBus>();
            eventBus.Subscribe<ActionReceivedIntegrationEvents, ActionReceivedIntegrationEventHandler>();
            eventBus.Subscribe<ActionAccountValidationCompleteIntegrationEvent, ActionAccountValidationCompleteIntegrationEventHandler>();
            eventBus.Subscribe<ProgramEligibilityConfirmedIntegrationEvent, ProgramEligibilityConfirmedIntegrationEventHandler>();
            eventBus.Subscribe<ProgramEligibilityRejectedIntegrationEvent, ProgramEligibilityRejectedIntegrationEventHandler>();
            eventBus.Subscribe<ActionRewardsConfirmedIntegrationEvent, ActionRewardsConfirmedIntegrationEventHandler>();
            eventBus.Subscribe<ActionRewardsRejectedIntegrationEvent, ActionRewardsRejectedIntegrationEventHandler>();
        }
    }
}

