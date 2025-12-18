using Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Settings;

namespace InversionOfControl
{
    public static class Dependencies
    {
        //services.AddDbContext -> já faz a mesma função do AddScoped.
        //services.AddSingleton -> Provem uma intancia do objeto para aplicação toda, sempre ativa.
        //services.AddScoped -> Busca sempre da memoria caso exista, se não cria.
        //services.AddTransient -> Cada requisição cria uma nova instância.

        public static void Start(IServiceCollection services)
        {
            //Faz a dependência do ImemoryCache, caso seja necessário utilizar no projeto
            //services.AddSingleton<IMemoryCache, MemoryCache>();

            Contexts(services);
            Domain(services);
            Data(services);
            DataService(services);
        }

        private static void Contexts(IServiceCollection services)
        {
            /**********************************************************************************************************************
            ATENÇÃO: Deixar em ordem alfabética 
            **********************************************************************************************************************/

            services.AddDbContext<ContextDefault>(options => options.UseSqlServer(SettingApp.ConnectionStrings.Default)
              .UseLoggerFactory(LoggerFactory.Create(builder => builder.AddConsole()))
           );
        }

        private static void Domain(IServiceCollection services)
        {
            /**********************************************************************************************************************
            ATENÇÃO: Deixar em ordem alfabética 
            **********************************************************************************************************************/

            //services.AddScoped<IExemplo, HandlerExemplo>();

        }

        private static void Data(IServiceCollection services)
        {
            /**********************************************************************************************************************
            ATENÇÃO: Deixar em ordem alfabética 
            **********************************************************************************************************************/

            //services.AddScoped<IExemplo, HandlerExemplo>();
        }

        private static void DataService(IServiceCollection services)
        {
            /**********************************************************************************************************************
            ATENÇÃO: Deixar em ordem alfabética 
            **********************************************************************************************************************/

            //services.AddScoped<IExemplo, HandlerExemplo>();

        }

    }
}
