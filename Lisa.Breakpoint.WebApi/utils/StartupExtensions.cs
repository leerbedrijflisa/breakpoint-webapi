using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Diagnostics;
using Raven.Client;
using Raven.Client.Document;
using System;
using System.IdentityModel.Tokens;
using Newtonsoft.Json;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Lisa.Breakpoint.WebApi
{
    public static class StartupExtensions
    {
        public static IServiceCollection AddBreakpointDB(this IServiceCollection services)
        {
            var docStore = new DocumentStore() { Url = "http://localhost:8080", DefaultDatabase = "breakpoint" };
            docStore.Conventions.SaveEnumsAsIntegers = true;
            services.AddInstance<IDocumentStore>(docStore);
            services.AddTransient<RavenDB>();
            return services;
        }

        public static IApplicationBuilder UseBreakpointJWTAuthentication(this IApplicationBuilder app, RsaSecurityKey key, TokenAuthOptions tokenOptions)
        {
            // Register a simple error handler to catch token expiries and change them to a 401, 
            // and return all other errors as a 500. This should almost certainly be improved for
            // a real application.
            app.UseExceptionHandler(appBuilder =>
            {
                appBuilder.Use(async (context, next) =>
                {
                    var error = context.Features[typeof(IExceptionHandlerFeature)] as IExceptionHandlerFeature;
                    // This should be much more intelligent - at the moment only expired 
                    // security tokens are caught - might be worth checking other possible 
                    // exceptions such as an invalid signature.
                    if (error != null && error.Error is SecurityTokenExpiredException)
                    {
                        context.Response.StatusCode = 401;
                        // What you choose to return here is up to you, in this case a simple 
                        // bit of JSON to say you're no longer authenticated.
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(
                            JsonConvert.SerializeObject(
                                new { authenticated = false, tokenExpired = true }));
                    }
                    else if (error != null && error.Error != null)
                    {
                        context.Response.StatusCode = 500;
                        context.Response.ContentType = "application/json";
                        // TODO: Shouldn't pass the exception message straight out, change this.
                        await context.Response.WriteAsync(
                            JsonConvert.SerializeObject
                            (new { success = false, error = error.Error.Message }));
                    }
                    // We're not trying to handle anything else so just let the default 
                    // handler handle.
                    else
                    {
                        await next();
                    }
                });
            });

            return app.UseJwtBearerAuthentication(options =>
            {
                // Basic settings - signing key to validate with, audience and issuer.
                options.TokenValidationParameters.IssuerSigningKey = key;
                options.TokenValidationParameters.ValidAudience = tokenOptions.Audience;
                options.TokenValidationParameters.ValidIssuer = tokenOptions.Issuer;

                // When receiving a token, check that we've signed it.
                options.TokenValidationParameters.ValidateSignature = true;

                // When receiving a token, check that it is still valid.
                options.TokenValidationParameters.ValidateLifetime = true;

                // This defines the maximum allowable clock skew - i.e. provides a tolerance on the token expiry time 
                // when validating the lifetime. As we're creating the tokens locally and validating them on the same 
                // machines which should have synchronised time, this can be set to zero. Where external tokens are
                // used, some leeway here could be useful.
                options.TokenValidationParameters.ClockSkew = TimeSpan.FromMinutes(0);
            });
        }
    }

    public class ConfigurationExtensions
    {

    }
}
