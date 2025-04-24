using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Logging;
using Microsoft.OpenApi.Models;
using WebApplication1.Data.Database;
using WebApplication1.Data.Interfaces;
using WebApplication1.Data.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.HttpOverrides;
using System.Security.Authentication;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace WebApplication1;

public class Program
{
    [Obsolete]
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Настройки аутентификации с таймаутом 30 минут
        //builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        //    .AddCookie(options =>
        //    {
        //        options.LoginPath = "/Account/Login"; // Перенаправление на страницу авторизации
        //        options.AccessDeniedPath = "/Account/Login"; // Если нет доступа, также редирект на Login
        //        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Таймаут авторизации
        //        options.SlidingExpiration = true; // Обновляет таймер при активности
        //    });

        // Включаем Newtonsoft.Json для Npgsql (jsonb)
        NpgsqlConnection.GlobalTypeMapper.UseJsonNet();


        // Add services to the container.
        builder.Services.AddControllersWithViews();
        builder.Services.AddControllers();
        builder.Configuration.AddEnvironmentVariables();

        // Add DBContext to conect to DB.
        //builder.Services.AddDbContextFactory<MainDBContext>(options =>
        //    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")), ServiceLifetime.Scoped);

        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
        ?? builder.Configuration.GetConnectionString("DefaultConnection");

        builder.Services.AddDbContextFactory<MainDBContext>(options =>
        options.UseNpgsql(connectionString), ServiceLifetime.Scoped);

        //builder.Services.AddDbContextFactory<MainDBContext>(options =>
        //    options.UseNpgsql("Host=localhost;Port=5432;Database=k8sapi;Username=apiuser;Password=yourStrongPassword123"), ServiceLifetime.Scoped);

        //builder.Services.AddDbContext<MainDBContext>(options => 
        //    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")), ServiceLifetime.Transient, ServiceLifetime.Transient);      

        //Add DBWorker for DB proccess
        builder.Services.AddTransient<IDBService, DBWorker>();

        //Add services for proxmox and rancher
        builder.Services.AddScoped<IProxmoxService, ProxmoxService>();
        builder.Services.AddScoped<IRancherService, RancherService>();
        builder.Services.AddScoped<IConnectionService, ConnectionService>();
        builder.Services.AddScoped<IProvisionService, ProvisionService>();
        builder.Services.AddScoped<IJiraService, JiraService>();

        builder.WebHost.ConfigureKestrel(options =>
        {
            //options.ListenAnyIP(8080); // HTTP

            //options.ListenAnyIP(8081, listenOptions =>
            //{
            //    listenOptions.UseHttps("/app/cert/tls.pfx", "");
            //});

            options.ConfigureHttpsDefaults(httpsOptions =>
            {
                httpsOptions.SslProtocols = SslProtocols.Tls12;
                httpsOptions.SslProtocols = SslProtocols.Tls13;
            });
        });

        // adding JWT Auth
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = "oidc";
        })
        .AddCookie()
        .AddOpenIdConnect("oidc", options =>
        {
            options.RequireHttpsMetadata = false;
            options.Authority = builder.Configuration.GetValue<string>("DEX_URL"); ;
            options.ClientId = builder.Configuration.GetValue<string>("DEX_CLIENT_ID");
            options.ClientSecret = builder.Configuration.GetValue<string>("DEX_CLIENT_SECRET");            

            options.ResponseType = "code";
            options.SaveTokens = true;
            options.Scope.Add("openid");
            options.Scope.Add("email");
            options.Scope.Add("profile");
            //options.Scope.Add("offline_access");
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.Authority = builder.Configuration.GetValue<string>("DEX_URL"); // Dex issuer URL
            options.Audience = builder.Configuration.GetValue<string>("DEX_CLIENT_ID"); // Must match Dex client ID
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                SaveSigninToken = true

                // You can add more customization if needed
            };
        });

        builder.Services.AddAuthorization();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.EnableAnnotations();
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Rancher To Proxmox API", Version = "v1" });

            var basePath = AppContext.BaseDirectory;

            var xmlPath = Path.Combine(basePath, "RancherToProxmoxAPI.xml");
            c.IncludeXmlComments(xmlPath);

            // Добавляем поддержку авторизации через Bearer
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                In = ParameterLocation.Header,
                Description = "Insert toket in format: Bearer {token}",
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        IdentityModelEventSource.ShowPII = true;
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        
        //app.UseHttpsRedirection();
        app.UseStaticFiles();

        var forwardedHeadersOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };
        forwardedHeadersOptions.KnownNetworks.Clear(); // иначе блокирует из-за неизвестной подсети
        forwardedHeadersOptions.KnownProxies.Clear();  // аналогично

        app.UseForwardedHeaders(forwardedHeadersOptions);


        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        });

        app.Run();
    }
}
