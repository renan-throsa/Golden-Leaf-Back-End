﻿using Golden_Leaf_Back_End.Filters;
using Golden_Leaf_Back_End.Models;
using Golden_Leaf_Back_End.Models.CategoryModels;
using Golden_Leaf_Back_End.Models.ClerkModels;
using Golden_Leaf_Back_End.Models.ClientModels;
using Golden_Leaf_Back_End.Models.OrderModels;
using Golden_Leaf_Back_End.Models.PaymentModels;
using Golden_Leaf_Back_End.Models.ProductModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System;
using System.Text;

namespace Golden_Leaf_Back_End
{
    public class Startup
    {
        private readonly IConfiguration configuration;
        private readonly IWebHostEnvironment environment;


        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            this.configuration = configuration;
            this.environment = environment;

        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var variables = configuration.GetSection("Development").Get<Variables>();

            services.AddDbContextPool<GoldenLeafContext>(options =>
            {
                if (environment.IsDevelopment())
                {
                    options.UseSqlServer(variables.Connection);
                }

                if (environment.IsProduction())
                {
                    variables = configuration.GetSection("Production").Get<Variables>();
                    options.UseNpgsql(variables.Connection);
                }
            });

            services.AddSingleton(variables);
            //Identity
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = null; //Validation will be done in the model;
            })
                .AddEntityFrameworkStores<GoldenLeafContext>()
                .AddDefaultTokenProviders();

            //Injection
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<IClientRepository, ClientRepository>();
            services.AddScoped<IOrderRepository, OrderRepository>();
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            

            //CORS Policy
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyMethod().AllowAnyHeader().WithOrigins(variables.Audience);
                });
            });

            //Authentication
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = variables.Issuer,

                        ValidateAudience = true,
                        ValidAudience = variables.Audience,

                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,

                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(variables.Key)),

                    };
                }
            );


            //I want to use my own state validation implemented in my exception filter.
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            services.AddApiVersioning();

            services.AddControllers(options =>
            {
                options.Filters.Add(typeof(ErrorResponseFilter));
                //options.Filters.Add(new AuthorizeResponseFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));

            }).AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });


            //APi documentation
            services.AddSwaggerGen(options =>
            {
                options.EnableAnnotations();

                //Fix enums conflicts.
                options.CustomSchemaIds(type => type.FullName);

                // definition of the security scheme used
                options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme (Example: 'Bearer 12345abcdef')",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = JwtBearerDefaults.AuthenticationScheme
                });
                //Defines what operations use the abome scheme - (all).
                options.AddSecurityRequirement(new OpenApiSecurityRequirement{
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = JwtBearerDefaults.AuthenticationScheme
                        }
                    },
                    Array.Empty<string>()
                }});


                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Golden Leaf api",
                    Description = "Documentação da api de estoque de produtos.",
                    Contact = new OpenApiContact
                    {
                        Name = "Renan Rosa",
                        Email = "renannojosa@gmail.com"
                    },
                });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Golden Leaf v1");
                    options.DefaultModelsExpandDepth(-1);

                });
            }

            app.UseCors();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
