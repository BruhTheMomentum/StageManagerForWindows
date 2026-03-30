using Microsoft.Win32;

namespace StageManager.Services
{
	public static class Settings
	{
		private const string REG_KEY = @"SOFTWARE\StageManager\Settings";

		public static void SetHideDesktopIcons(bool hideIcons)
		{
			using var key = Registry.CurrentUser.CreateSubKey(REG_KEY);
			key?.SetValue("HideDesktopIcons", hideIcons ? 1 : 0, RegistryValueKind.DWord);
		}

		public static bool GetHideDesktopIcons()
		{
			using var key = Registry.CurrentUser.OpenSubKey(REG_KEY);
			var raw = key?.GetValue("HideDesktopIcons");
			if (raw is int i) return i != 0;
			// Legacy: old versions stored bool as REG_SZ — reader always fell through to default true.
			// Treat legacy strings the same way so existing users keep their expected behavior.
			return true;
		}
	}
}