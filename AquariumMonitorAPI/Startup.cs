﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AquariumMonitor.BusinessLogic;
using AquariumMonitor.BusinessLogic.Interfaces;
using AquariumMonitor.API.Controllers;
using AquariumMonitor.DAL;
using AquariumMonitor.DAL.Interfaces;
using AutoMapper;
using BusinessLogic.Adapters;
using BusinessLogic.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Pwned;

namespace AquariumMonitor.API
{
    public class Startup
    {
        private IHostingEnvironment _env;
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            _env = env;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var tokenSecurityKey = Environment.GetEnvironmentVariable(Constants.TokenSecurityKey);

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = Configuration[Constants.ValidIssuer],
                        ValidAudience = Configuration[Constants.ValidAudience],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSecurityKey))
                    };
                });

            services.AddAuthorization(cfg => { cfg.AddPolicy("Admin", p => p.RequireClaim("Admin", "True")); });

            string[] origins = Configuration[Constants.CorsOrigins].Split(';');

            services.AddCors(cfg =>
            {
                cfg.AddPolicy(Configuration[Constants.CorsPolicyName], bldr =>
                {
                    bldr.AllowAnyHeader()
                        .AllowAnyMethod()
                        .WithOrigins(origins);
                });
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            // Add authenticated policy so it can be globally added to all controllers
            var authenticatedPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            services.AddMvc(opt =>
                {
                    if (!_env.IsProduction())
                    {
                        opt.SslPort = 44308;
                    }
                    opt.Filters.Add(new RequireHttpsAttribute());
                    opt.Filters.Add(new AuthorizeFilter(authenticatedPolicy));
                })
                .AddJsonOptions(opt =>
                {
                    opt.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
				}).SetCompatibilityVersion(CompatibilityVersion.Version_2_1);


            services.AddApiVersioning(cfg =>
            {
                cfg.DefaultApiVersion = new ApiVersion(1, 0);
                cfg.AssumeDefaultVersionWhenUnspecified = true;
                cfg.ReportApiVersions = true;
                cfg.ApiVersionReader = new HeaderApiVersionReader(Constants.APIVersionHeaderName);
            });

            // Add application services.
            services.AddScoped<IConnectionFactory>(s =>
                new ConnectionFactory(Configuration.GetConnectionString(Constants.DBConnectionName)));

            services.AddAutoMapper();
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IValidationManager, ValidationManager>();
            services.AddScoped<IHaveIBeenPwnedRestClient, HaveIBeenPwnedRestClient>();
            services.AddScoped<ILoggerAdapter<BaseController>, LoggerAdapter<BaseController>>();

            // Repositories
            services.AddScoped<IAquariumRepository, AquariumRepository>();
            services.AddScoped<IMeasurementRepository, MeasurementRepository>();
            services.AddScoped<IWaterChangeRepository, WaterChangeRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IAquariumTypeRepository, AquariumTypeRepository>();
            services.AddScoped<IUnitRepository, UnitRepository>();
            services.AddScoped<IPasswordRepository, PasswordRepository>();

            // Managers
            services.AddScoped<IUnitManager, UnitManager>();
            services.AddScoped<IAquariumTypeManager, AquariumTypeManager>();
            services.AddScoped<IMeasurementManager, MeasurementManager>();
            services.AddScoped<IPasswordManager, PasswordManager>();
            services.AddScoped<IAuthManager, AuthManager>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseAuthentication();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseCookiePolicy();
            app.UseCors(Configuration[Constants.CorsPolicyName]);
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
