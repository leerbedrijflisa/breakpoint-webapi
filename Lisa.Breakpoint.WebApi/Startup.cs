using Lisa.Breakpoint.WebApi.database;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Framework.DependencyInjection;
using System.IdentityModel.Tokens;
using Newtonsoft.Json.Serialization;
using Raven.Client;
using Raven.Client.Document;
using System.Security.Cryptography;
using Breakpoint.TokenAuthentication;

namespace Lisa.Breakpoint.WebApi
{
    public class Startup
    {
        const string TokenIssuer = "Breakpoint";
        private RsaSecurityKey key;
        private TokenAuthOptions tokenOptions;

        public void ConfigureServices(IServiceCollection services)
        {
            // *** CHANGE THIS FOR PRODUCTION USE ***
            // Here, we're generating a random key to sign tokens - obviously this means
            // that each time the app is started the key will change, and multiple servers 
            // all have different keys. This should be changed to load a key from a file 
            // securely delivered to your application, controlled by configuration.
            //
            // See the RSAKeyUtils.GetKeyParameters method for an example of loading from
            // a JSON file.
            RSAParameters keyParams = RSAKeyUtils.GetRandomKey();

            // Create the key, and a set of token options to record signing credentials 
            // using that key, along with the other parameters we will need in the 
            // token controlller.
            key = new RsaSecurityKey(keyParams);
            tokenOptions = new TokenAuthOptions()
            {
                Issuer = TokenIssuer,
                SigningCredentials = new SigningCredentials(key,
                    SecurityAlgorithms.RsaSha256Signature, SecurityAlgorithms.Sha256Digest)
            };

            // Save the token options into an instance so they're accessible to the 
            // controller.
            services.AddInstance<TokenAuthOptions>(tokenOptions);

            // Set MVC options
            services.AddMvc().AddJsonOptions(opts =>
            {
                opts.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                //opts.SerializerSettings.Converters.Add(new StringEnumConverter());
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
            app.UseCors("Breakpoint");
            app.UseMvcWithDefaultRoute();
        }
    }

    public static class StartupExtensions
    {
        public static IServiceCollection AddBreakpointDB(this IServiceCollection services)
        {
            var docStore = new DocumentStore() { Url = "http://localhost:8080", DefaultDatabase = "breakpoint" };
            docStore.Conventions.SaveEnumsAsIntegers = true;
            services.AddInstance<IDocumentStore>(docStore);
            services.AddSingleton<RavenDB>();
            return services;
        }
    }
}