using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using System.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Authentication.JwtBearer;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Lisa.Breakpoint.WebApi
{
    public class Startup
    {
        const string TokenIssuer = "Breakpoint";
        const string TokenAudience = "Breakpoint";
        private RsaSecurityKey key;
        private TokenAuthOptions tokenOptions;

        public void ConfigureServices(IServiceCollection services)
        {
            key = RSAKeyUtils.GetRSAKey();

            tokenOptions = new TokenAuthOptions()
            {
                Issuer = TokenIssuer,
                Audience = TokenAudience,
                SigningCredentials = new SigningCredentials(key,
                    SecurityAlgorithms.RsaSha256Signature)
            };

            // Save the token options into an instance so they're accessible to the 
            // controller.
            services.AddInstance<TokenAuthOptions>(tokenOptions);

            services.AddAuthorization(auth =>
            {
                auth.AddPolicy("Bearer", new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme‌​)
                    .RequireAuthenticatedUser().Build());
            });

            // Set MVC options
            services.AddMvc().AddJsonOptions(opts =>
            {
                opts.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                //opts.SerializerSettings.Converters.Add(new StringEnumConverter());
                opts.SerializerSettings.DefaultValueHandling = DefaultValueHandling.Ignore;
                opts.SerializerSettings.MissingMemberHandling = MissingMemberHandling.Error;
            });

            // Add CORS policies
            services.AddCors(options =>
            {
                options.AddPolicy("Breakpoint", builder => 
                {
                    builder.WithOrigins("*").AllowAnyHeader().AllowAnyMethod();
                });
            });

            services.AddBreakpointDB();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            app.UseIISPlatformHandler();

            app.UseBreakpointJWTAuthentication(key, tokenOptions);
            app.UseCors("Breakpoint");

            app.UseMvcWithDefaultRoute();
        }
    }
}