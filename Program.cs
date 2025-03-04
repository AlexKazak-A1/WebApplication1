using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using WebApplication1.Data.Database;
using WebApplication1.Data.Interfaces;
using WebApplication1.Data.Services;

namespace WebApplication1;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddControllersWithViews();
        builder.Services.AddControllers();
        builder.Configuration.AddEnvironmentVariables();

        // Add DBContext to conect to DB.
        builder.Services.AddDbContext<MainDBContext>(options => 
            options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")), ServiceLifetime.Transient, ServiceLifetime.Transient);

        //Add DBWorker for DB proccess
        builder.Services.AddTransient<IDBService, DBWorker>();

        //Add services for proxmox and rancher
        builder.Services.AddScoped<IProxmoxService, ProxmoxService>();
        builder.Services.AddScoped<IRancherService, RancherService>();
        builder.Services.AddScoped<IConnectionService, ConnectionService>();
        builder.Services.AddScoped<IProvisionService, ProvisionService>();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.EnableAnnotations();
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Rancher To Proxmox API", Version = "v1" });

            var basePath = AppContext.BaseDirectory;

            var xmlPath = Path.Combine(basePath, "RancherToProxmoxAPI.xml");
            c.IncludeXmlComments(xmlPath);
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        
        app.UseHttpsRedirection();
        app.UseStaticFiles();

        
        

        app.UseRouting();

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
