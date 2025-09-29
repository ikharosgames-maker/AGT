using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Agt.Infrastructure.DI;             // AddAgtCore

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

            Services = sc.BuildServiceProvider();

            var main = new Views.MainShell();
            main.Show();
        }
    }
}
