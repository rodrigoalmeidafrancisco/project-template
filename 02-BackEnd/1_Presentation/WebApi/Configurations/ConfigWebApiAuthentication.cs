using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shared.Settings;
using System.Text;

namespace WebApi.Configurations
{
    public static class ConfigWebApiAuthentication
    {
        extension(WebApplicationBuilder builder)
        {
            public void AddAuthentication()
            {
                builder.Services.AddAuthentication(x =>
                {
                    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(x =>
                {
                    x.Authority = $"Template_{SettingApp.Aplication._Environment}";
                    x.RequireHttpsMetadata = false;
                    x.SaveToken = true;
                    x.TokenValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes($"{SettingApp.Aplication._Environment.ToUpper()}{SettingApp.Constants.TokenKey}")),
                        ValidateIssuer = false,
                        ValidateAudience = false
                    };

                    x.Events = new JwtBearerEvents
                    {
                        OnChallenge = context =>
                        {
                            context.HandleResponse();
                            context.Response.StatusCode = 401;
                            context.Response.ContentType = "application/json";
                            return context.Response.WriteAsync("{\"message\": \"Não Autorizado\"}");
                        },
                        OnForbidden = context =>
                        {
                            context.Response.StatusCode = 403;
                            context.Response.ContentType = "application/json";
                            return context.Response.WriteAsync("{\"message\": \"Acesso Proibido\"}");
                        }
                    };
                });

                //Configuração dos "scope" de acesso e "apolicy" na aplicação
                if (SettingApp.Aplication.ListAccessPolicy.Count != 0)
                {
                    builder.Services.AddAuthorization(options =>
                    {
                        SettingApp.Aplication.ListAccessPolicy.ForEach(item => { options.AddPolicy(item.Key, policy => { policy.RequireClaim("scope", item.Value); }); });
                    });
                }
            }
        }
    }
}
