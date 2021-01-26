using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using WallTec.CoreCom.Server;
using WallTec.CoreCom.Server.Models;
using WallTec.CoreCom.TestServerAppService.Service;

namespace WallTec.CoreCom.TestServerAppService
{
    public class Startup
    {
        private readonly JwtSecurityTokenHandler JwtTokenHandler = new JwtSecurityTokenHandler();
        private readonly SymmetricSecurityKey SecurityKey = new SymmetricSecurityKey(Guid.NewGuid().ToByteArray());

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddGrpc(options => 
            {
                options.EnableDetailedErrors = true;
                options.MaxReceiveMessageSize = null; //When set to null, the message size is unlimited. or 2 * 1024 * 1024; // 2 MB
                options.MaxSendMessageSize = null; //When set to null, the message size is unlimited.

            });
            //This is needed we only have one instance of CoreComOptions
            services.AddSingleton<CoreComOptions>();

            //If you would like to use scoped remove this lines,you need to injects a Scoped service,
            //services.AddSingleton<MyService>();
            
            services.AddAuthorization(options =>
            {
                options.AddPolicy(JwtBearerDefaults.AuthenticationScheme, policy =>
                {
                    policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                    policy.RequireClaim(ClaimTypes.Name);
                });
            });
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateAudience = false,
                            ValidateIssuer = false,
                            ValidateActor = false,
                            ValidateLifetime = true,
                            IssuerSigningKey = SecurityKey
                        };
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

            //Warm it up to get settings from appsettings.json 
            app.ApplicationServices.GetService<CoreComOptions>();
           

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseGrpcWeb();

           
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<MyService>().EnableGrpcWeb();
                endpoints.MapGet("/generateJwtToken", context =>
                {
                    return context.Response.WriteAsync(GenerateJwtToken(context.Request.Query["username"], context.Request.Query["password"]));
                });
            });

            
        }

        private String GenerateJwtToken(string username, string password)
        {
            string clientId_From_Db;
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                throw new InvalidOperationException("Name or password is not specified.");
            }

            if ((username != "demoIos" || username != "demoDroid") && password != "1234")
            {
                throw new InvalidOperationException("Name or password is not specified.");
            }
            else
            {
                if (username != "demoIos")
                    clientId_From_Db = "Ios_clientid";
                else
                    clientId_From_Db = "Droid_clientid";
            }
            var claims = new[] { new Claim(ClaimTypes.Name, username), new Claim("ClientId", clientId_From_Db) };
            var credentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken("ExampleServer", "ExampleClients", claims, expires: DateTime.Now.AddMinutes(60), signingCredentials: credentials);
            return String.Format("{0}|{1}", JwtTokenHandler.WriteToken(token), clientId_From_Db);
            
        }
    }
}
