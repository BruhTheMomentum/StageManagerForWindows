using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace StageManager
{
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			Services.ThemeManager.ApplyTheme();

			// Log-only — intentionally NOT setting args.Handled so the app terminates
			DispatcherUnhandledException += (s, args) =>
			{
				Log.Fatal("CRASH", $"UI thread: {args.Exception}");
			};

			AppDomain.CurrentDomain.UnhandledException += (s, args) =>
			{
				Log.Fatal("CRASH", $"Unhandled: {args.ExceptionObject}");
			};

			TaskScheduler.UnobservedTaskException += (s, args) =>
			{
				Log.Fatal("CRASH", $"Unobserved task: {args.Exception}");
			};
		}
	}
}
