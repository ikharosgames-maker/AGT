using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Agt.Domain.Repositories;
using Agt.Infrastructure.JsonStore;

namespace Agt.Desktop
{
    public partial class App : System.Windows.Application
    {
        public static IServiceProvider Services { get; private set; } = default!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var sc = new ServiceCollection();
            sc.AddSingleton<ICaseDataRepository, JsonCaseDataRepository>();
            Services = sc.BuildServiceProvider();
        }
    }
}
