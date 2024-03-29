﻿using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Eureka;
using Prometheus;
using Steeltoe.Discovery.Client;
using Steeltoe.Discovery.Eureka;
using System.Diagnostics;
using System.Text;

namespace Gateway
{
    public class Startup
    {
        private IWebHostEnvironment _env;
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            _env = env;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.SetIsOriginAllowed((host) => true)
                        //.WithOrigins("http://localhost:19006/")
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "RestAPI", Version = "v1" });
            });

            services.AddOcelot()
                .AddEureka();
            services.AddDiscoveryClient();
            services.AddServiceDiscovery(options => options.UseEureka());

            services.AddAuthentication(authentication =>
            {
                authentication.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                authentication.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = null,
                        ValidAudience = null,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration.GetValue<string>("JWTSecurityKey")))
                    };
                });
            services.AddAuthorization();
            Trace.WriteLine(Configuration.GetValue<string>("JWTSecurityKey"));

            //string connectionstring;
            //if (_env.IsDevelopment()) connectionstring = Configuration.GetConnectionString("DevConnection");
            //else connectionstring = Configuration.GetConnectionString("DefaultConnection");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "RestAPI v1"));

            app.UseRouting();
            app.UseHttpMetrics();

            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseDiscoveryClient();
            app.UseOcelot().Wait();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapMetrics();
            });
        }
    }
}
