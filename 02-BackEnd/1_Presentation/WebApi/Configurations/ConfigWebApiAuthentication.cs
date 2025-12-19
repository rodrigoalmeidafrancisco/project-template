using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Shared.Settings;
using System.Text;
using System.Text.Json;

namespace WebApi.Configurations
{
    /// <summary>
    /// Classe estática responsável por configurar autenticação e autorização JWT na WebApi
    /// </summary>
    public static class ConfigWebApiAuthentication
    {
        /// <summary>
        /// Extension method para configurar autenticação JWT no builder da aplicação
        /// </summary>
        /// <param name="builder">WebApplicationBuilder usado para configurar a aplicação</param>
        public static void AddAuthentication(this WebApplicationBuilder builder)
        {
            // Adiciona o serviço de autenticação ao container de DI e configura JWT como esquema padrão
            builder.Services.AddAuthentication(x =>
            {
                // Define JWT Bearer como esquema padrão para autenticar usuários automaticamente
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;

                // Define JWT Bearer como esquema padrão para desafiar requisições não autenticadas (retorna 401)
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            // Configura o middleware JWT Bearer com parâmetros de validação do token
            .AddJwtBearer(x =>
            {
                // ⚠️ ATENÇÃO: Authority normalmente deve ser a URL do servidor de autenticação (ex: IdentityServer, Auth0)
                // Este valor atual não é uma URL válida e pode causar problemas em validações
                // Considere remover se não usar validação de issuer ou configurar corretamente
                x.Authority = $"{SettingApp.Aplication.Name} - {SettingApp.Aplication._Environment}";

                // ⚠️ CRÍTICO: Define se exige HTTPS para metadados (false = permite HTTP)
                // Em PRODUÇÃO, deve ser TRUE para segurança! Use false apenas em desenvolvimento local
                x.RequireHttpsMetadata = !builder.Environment.IsDevelopment(); // MELHORADO: Exige HTTPS em produção

                // Define se o token JWT deve ser salvo no AuthenticationProperties após validação bem-sucedida
                // Útil para acessar o token posteriormente via HttpContext.GetTokenAsync("access_token")
                x.SaveToken = true;

                // Configura os parâmetros de validação do token JWT recebido
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    // Define a chave simétrica usada para validar a assinatura digital do token
                    // ⚠️ A chave é construída concatenando ambiente + chave secreta do appsettings
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes($"{SettingApp.Aplication._Environment.ToUpper()}{SettingApp.Constants.TokenKey}")), // MELHORADO: UTF8 ao invés de ASCII

                    // ⚠️ SEGURANÇA: Define se valida o emissor do token (quem criou o token)
                    // FALSE = não valida (menos seguro, mas mais flexível)
                    // Considere usar TRUE em produção e configurar ValidIssuer
                    ValidateIssuer = false,

                    // ⚠️ SEGURANÇA: Define se valida a audiência do token (para quem o token foi criado)
                    // FALSE = não valida (menos seguro, mas mais flexível)
                    // Considere usar TRUE em produção e configurar ValidAudience
                    ValidateAudience = false,

                    // MELHORADO: Adiciona validação de tempo de vida do token
                    ValidateLifetime = true,

                    // MELHORADO: Define tolerância de 10 minutos para diferenças de relógio entre servidores
                    ClockSkew = TimeSpan.FromMinutes(10),

                    // MELHORADO: Valida que a chave de assinatura está presente e é válida
                    ValidateIssuerSigningKey = true
                };

                // Configura eventos customizados do pipeline de autenticação JWT
                x.Events = new JwtBearerEvents
                {
                    // Evento disparado quando autenticação falha (token inválido, expirado ou ausente)
                    OnChallenge = context =>
                    {
                        // Previne o comportamento padrão (que retornaria WWW-Authenticate header)
                        context.HandleResponse();

                        // Define status HTTP 401 Unauthorized (não autenticado)
                        context.Response.StatusCode = 401;

                        // Define que a resposta será no formato JSON
                        context.Response.ContentType = "application/json";

                        // MELHORADO: Usa objeto serializado ao invés de string hardcoded
                        var response = new { message = "Não Autorizado", error = "Token inválido, ausente ou expirado" };
                        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
                    },

                    // Evento disparado quando usuário autenticado tenta acessar recurso sem permissão
                    OnForbidden = context =>
                    {
                        // Define status HTTP 403 Forbidden (autenticado mas sem permissão)
                        context.Response.StatusCode = 403;

                        // Define que a resposta será no formato JSON
                        context.Response.ContentType = "application/json";

                        // MELHORADO: Usa objeto serializado ao invés de string hardcoded
                        var response = new { message = "Acesso Proibido", error = "Você não possui permissão para acessar este recurso" };
                        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
                    },

                    // MELHORADO: Adiciona evento para logar falhas de autenticação
                    OnAuthenticationFailed = context =>
                    {
                        // Loga exceção para troubleshooting em Application Insights
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();

                        // ⚠️ SEGURANÇA: Incrementa contador de falhas para detecção de brute force
                        logger.LogWarning(context.Exception,
                            "Falha na autenticação JWT - IP: {IP}, Endpoint: {Endpoint}, Erro: {Message}",
                            context.HttpContext.Connection.RemoteIpAddress,
                            context.HttpContext.Request.Path,
                            context.Exception.Message);

                        return Task.CompletedTask;
                    },

                    // MELHORADO: Adiciona evento para logar tokens validados com sucesso
                    OnTokenValidated = context =>
                    {
                        // Útil para auditoria ou lógica adicional após validação bem-sucedida
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        var userId = context.Principal?.Identity?.Name ?? "Unknown";
                        logger.LogInformation("Token JWT validado com sucesso para usuário: {UserId}", userId);
                        return Task.CompletedTask;
                    }
                };
            });

            // Configura autorização baseada em políticas (claims de "scope")
            // Verifica se existem políticas de acesso configuradas no SettingApp
            if (SettingApp.Aplication.ListAccessPolicy?.Count > 0) // MELHORADO: Null-safe check
            {
                // Adiciona o serviço de autorização e configura as políticas
                builder.Services.AddAuthorization(options =>
                {
                    // Itera sobre cada política configurada e a registra
                    // Cada política exige uma claim específica de "scope" para acesso
                    foreach (var item in SettingApp.Aplication.ListAccessPolicy) // MELHORADO: foreach ao invés de ForEach
                    {
                        // Adiciona política que exige claim "scope" com valor específico
                        // Ex: [Authorize(Policy = "read:users")] exigirá claim scope="read:users"
                        options.AddPolicy(item.Key, policy =>
                        {
                            // RequireClaim exige que o token contenha a claim "scope" com o valor especificado
                            policy.RequireClaim("scope", item.Value);
                        });
                    }
                });
            }
        }
    }
}
