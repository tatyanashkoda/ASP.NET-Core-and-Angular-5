﻿using System;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CourseProject.Data.Model.Context;
using CourseProject.Infrastructure;
using CourseProject.Infrastructure.Filter;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using StructureMap;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CourseProject
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            string connection = Configuration.GetConnectionString("DefaultConnection");

            services.AddDbContext<ApplicationContext>(options =>
                options.UseSqlServer(connection));

           // services.AddMvc().AddFluentValidation();
            services.AddMvc()
                .AddControllersAsServices();
            //services.AddMvc(options => { options.Filters.Add(new ApiExceptionFilter()); });
            services.AddScoped<ApiExceptionFilterAttribute>();
            services.AddApplicationInsightsTelemetry(Configuration);
            services.AddMvc(
                config => {
                    config.Filters.Add(new ApiExceptionFilterAttribute());
                }
            );
            return ConfigureIoC(services);
        }

        public IServiceProvider ConfigureIoC(IServiceCollection services)
        {
            var container = new Container();

            container.Configure(config =>
            {
                // Register stuff in container, using the StructureMap APIs...
                config.Scan(_ =>
                {
                    _.AssemblyContainingType(typeof(Startup));
                    _.AssembliesFromPath(Environment.CurrentDirectory);
                    _.WithDefaultConventions();
                    _.ConnectImplementationsToTypesClosing(typeof(IValidator<>));
                    _.ConnectImplementationsToTypesClosing(typeof(IRequestHandler<>)); // Handlers with no response
                    _.ConnectImplementationsToTypesClosing(typeof(IRequestHandler<,>)); // Handlers with a response
                    _.ConnectImplementationsToTypesClosing(typeof(INotificationHandler<>));
                    _.ConnectImplementationsToTypesClosing(typeof(IPipelineBehavior<,>));
                });

                config.For<SingleInstanceFactory>().Use<SingleInstanceFactory>(ctx => t => ctx.GetInstance(t));
                config.For<MultiInstanceFactory>().Use<MultiInstanceFactory>(ctx => t => ctx.GetAllInstances(t));
                config.For<IMediator>().Use<Mediator>();

                //Populate the container using the service collection
                config.Populate(services);
                

            });

            return container.GetInstance<IServiceProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            //app.UseExceptionHandler(
            //    options => {
            //        options.Run(
            //            async context =>
            //            {
            //                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            //                context.Response.ContentType = "text/html";
            //                var ex = context.Features.Get<IExceptionHandlerFeature>();
            //                if (ex != null)
            //                {
            //                    var err = $"<h1>Error: {ex.Error.Message}</h1>{ex.Error.StackTrace }";
            //                    await context.Response.WriteAsync(err).ConfigureAwait(false);
            //                }
            //            });
            //    }
            //);
            app.UseMvc();
        }
    }
}
