using Agt.Desktop.Services;
using Agt.Domain.Repositories;
using Agt.Infrastructure.DI;             // AddAgtCore
using Agt.Infrastructure.JsonStore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace Agt.Desktop
{
    public partial class App 
    {
        public static IServiceProvider Services { get; private set; } = default!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var sc = new ServiceCollection();

            // Registrace aplikačních služeb + JSON úložiště (tvé rozšíření v Infrastructure)
            sc.AddAgtCore(useJsonStore: true);
            ConfigureServices(sc);
            Services = sc.BuildServiceProvider();

            var main = new Views.MainShell();
            main.Show();
        }
        private static void ConfigureServices(IServiceCollection services)
        {
            // === Repozitáře / služby, které používá UI ===
            // Per-case data (nové úložiště pro hodnoty uživatele)
            services.AddSingleton<ICaseDataRepository, JsonCaseDataRepository>();

            // Publikované formuláře / verze / bloky / routy
            // POZN: pokud máš jiné názvy tříd, přepiš je sem.
            services.AddSingleton<IFormRepository, JsonFormRepository>();
            services.AddSingleton<IBlockRepository, JsonBlockRepository>();
            services.AddSingleton<IRouteRepository, JsonRouteRepository>();
            services.AddSingleton<ICaseDataRepository, JsonCaseDataRepository>();
            services.AddSingleton<IFormVersioningService, FormVersioningService>();
            services.AddSingleton<IFormCloneService, FormCloneService>();
            services.AddSingleton<IFormDiffService, FormDiffService>();
            services.AddSingleton<IFormSaveService, FormSaveService>();
            services.AddSingleton<IBlockLibrary>(sp => BlockLibraryJson.Default);
            services.AddSingleton<IFormCaseRegistryService, FormCaseRegistryService>();
            // (Volitelné) pokud už máš backendové služby, můžeš je registrovat taky:
            // services.AddSingleton<ICaseService, MyCaseService>();
            // services.AddSingleton<IAssignmentService, MyAssignmentService>();

            // === Příprava na MSSQL ===
            // Až přepneš na DB, stačí tady vyměnit:
            // services.AddSingleton<IFormRepository,  SqlFormRepository>();
            // services.AddSingleton<IBlockRepository, SqlBlockRepository>();
            // services.AddSingleton<IRouteRepository, SqlRouteRepository>();
            // services.AddSingleton<ICaseDataRepository, SqlCaseDataRepository>();
        }
    }
}
