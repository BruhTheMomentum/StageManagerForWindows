using Microsoft.Win32;

namespace StageManager
{
	public static class Settings
	{
		private const string REG_KEY = @"SOFTWARE\StageManager\Settings";

		public static void SetHideDesktopIcons(bool hideIcons)
		{
			using var key = Registry.CurrentUser.CreateSubKey(REG_KEY);
			key?.SetValue("HideDesktopIcons", hideIcons);
		}

		public static bool GetHideDesktopIcons()
		{
			using var key = Registry.CurrentUser.OpenSubKey(REG_KEY);
			if (key?.GetValue("HideDesktopIcons") is bool value)
				return value;

			// Default to true (hide desktop icons)
			return true;
		}
	}
}