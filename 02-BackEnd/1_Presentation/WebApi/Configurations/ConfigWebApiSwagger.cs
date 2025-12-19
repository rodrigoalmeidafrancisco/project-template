using Microsoft.OpenApi;
using Shared.Settings;
using System.Reflection;

namespace WebApi.Configurations
{
    public static class ConfigWebApiSwagger
    {
        extension(WebApplicationBuilder builder)
        {
            public void AddConfigSwagger()
            {
                builder.Services.AddSwaggerGen(x =>
                {
                    x.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Version = "v1",
                        Title = SettingApp.Aplication.Name,
                        Description = "WebApi Documentation",
                        Contact = new OpenApiContact
                        {
                            Name = "Support Team",
                            Email = "support@example.com"
                        },
                        License = new OpenApiLicense
                        {
                            Name = "MIT",
                            Url = new Uri("https://opensource.org/licenses/MIT")
                        }
                    });

                    x.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                    {
                        Description = "Insira o token JWT desta forma: Bearer seu-token-aqui",
                        Name = "Authorization",
                        Scheme = "Bearer",
                        BearerFormat = "JWT",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey
                    });

                    // Configuração mais robusta para documentação XML
                    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                    if (File.Exists(xmlPath))
                    {
                        x.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
                    }

                    x.UseInlineDefinitionsForEnums();
                    x.DescribeAllParametersInCamelCase();
                    x.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

                    // Ordenar ações por método HTTP
                    x.OrderActionsBy(apiDesc => $"{apiDesc.ActionDescriptor.RouteValues["controller"]}_{apiDesc.HttpMethod}");
                });
            }
        }

        extension(WebApplication app)
        {
            public void AddConfigSwagger()
            {
                app.UseSwagger();
                app.UseSwaggerUI(x =>
                {
                    x.SwaggerEndpoint("/swagger/v1/swagger.json", $"{SettingApp.Aplication.Name} WebApi V1");
                    x.RoutePrefix = "swagger";
                    x.DocumentTitle = $"{SettingApp.Aplication.Name} - API Documentation";
                    x.DisplayRequestDuration();
                    x.EnableDeepLinking();
                    x.EnableFilter();
                    x.EnableTryItOutByDefault();
                });
            }
        }
    }
}
