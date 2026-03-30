using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using StageManager.Native.PInvoke;

namespace StageManager.Controls
{
    public partial class IconOverlayWindow : Window
    {
        public IconOverlayWindow()
        {
            InitializeComponent();
        }

        public Canvas Canvas => IconCanvas;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var hwnd = new WindowInteropHelper(this).Handle;
            var exStyle = Win32.GetWindowExStyleLongPtr(hwnd);
            Win32.SetWindowStyleExLongPtr(hwnd, exStyle | Win32.WS_EX.WS_EX_TOOLWINDOW | Win32.WS_EX.WS_EX_TRANSPARENT);
        }
    }
}
