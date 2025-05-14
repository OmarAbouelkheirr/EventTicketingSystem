using System.Text;
using EventBookingSystem.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddDbContext<ApplicationDbContext>(opt =>
            {
                opt.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            builder.Services.AddControllersWithViews();

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                };
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var token = context.Request.Cookies["jwt"];
                        if (!string.IsNullOrEmpty(token))
                        {
                            context.Token = token;
                        }
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        // This is called on 401 Unauthorized
                        context.HandleResponse(); // Skip default behavior
                        context.Response.StatusCode = 401;
                        context.Response.Redirect("/UnAuthenticated.html"); // Your custom page
                        return Task.CompletedTask;
                    },
                    OnForbidden = context =>
                    {
                        // This is called on 403 Forbidden
                        context.Response.StatusCode = 403;
                        context.Response.Redirect("/UnAuthorized.html"); // Your custom page
                        return Task.CompletedTask;
                    }
                };

            });

           

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            
            app.UseRouting();
            app.UseAuthentication(); app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
      name: "default",
      pattern: "{controller=Account}/{action=HomePage}");

                //endpoints.MapControllers(); // This is what enables API controller routing
            });

            app.Run();
        }
    }
}
