using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using StageManager.Model;

namespace StageManager.Animations
{
	internal class SceneTransitionAnimator : IDisposable
	{
		private const int AnimationDurationMs = 300;

		private TransitionOverlayWindow _overlay;
		private bool _isAnimating;

		public bool IsAnimating => _isAnimating;

		internal TransitionOverlayWindow Overlay => _overlay;

		internal TransitionOverlayWindow GetOrCreateOverlay(Rect bounds)
		{
			EnsureOverlay(bounds);
			return _overlay;
		}

		/// <summary>
		/// Pre-creates the overlay window so the first animation has no HWND-creation lag.
		/// </summary>
		public void WarmUp(Rect bounds)
		{
			EnsureOverlay(bounds);
			_overlay.Show();
			_overlay.Hide();
			Log.Info("ANIM", "Overlay warmed up");
		}

		/// <summary>
		/// Animates placeholders for both the incoming and outgoing scenes simultaneously.
		/// Pass Rect.Empty for outgoingSource to skip the outgoing animation.
		/// </summary>
		public Task AnimateSceneTransitionAsync(
			Rect overlayBounds,
			Rect incomingSource, Rect incomingTarget, SceneModel incomingScene,
			Rect outgoingSource, Rect outgoingTarget, SceneModel outgoingScene)
		{
			if (_isAnimating) return Task.CompletedTask;
			_isAnimating = true;
			var tcs = new TaskCompletionSource<bool>();

			try
			{
				EnsureOverlay(overlayBounds);

				var overlayLeft = _overlay.Left;
				var overlayTop = _overlay.Top;
				var duration = new Duration(TimeSpan.FromMilliseconds(AnimationDurationMs));
				var easing = new PowerEase { EasingMode = EasingMode.EaseOut };
				var storyboard = new Storyboard();

				// Track placeholders so we can remove exactly these on completion
				Border inPlaceholder = null;
				Border outPlaceholder = null;

				// --- Incoming placeholder (sidebar → window position) ---
				var inIcon = incomingScene?.Windows.FirstOrDefault()?.Icon;
				inPlaceholder = PlaceholderFactory.Create(inIcon);
				SetupPlaceholder(inPlaceholder, storyboard, duration, easing, overlayLeft, overlayTop,
					incomingSource, incomingTarget);
				_overlay.Canvas.Children.Add(inPlaceholder);

				Log.Info("ANIM", $"Incoming: ({incomingSource.X - overlayLeft:F0},{incomingSource.Y - overlayTop:F0} {incomingSource.Width:F0}x{incomingSource.Height:F0}) → ({incomingTarget.X - overlayLeft:F0},{incomingTarget.Y - overlayTop:F0} {incomingTarget.Width:F0}x{incomingTarget.Height:F0})");

				// --- Outgoing placeholder (window position → sidebar) ---
				if (outgoingSource != Rect.Empty && outgoingScene != null)
				{
					var outIcon = outgoingScene.Windows.FirstOrDefault()?.Icon;
					outPlaceholder = PlaceholderFactory.Create(outIcon);
					SetupPlaceholder(outPlaceholder, storyboard, duration, easing, overlayLeft, overlayTop,
						outgoingSource, outgoingTarget);
					_overlay.Canvas.Children.Add(outPlaceholder);

					Log.Info("ANIM", $"Outgoing: ({outgoingSource.X - overlayLeft:F0},{outgoingSource.Y - overlayTop:F0} {outgoingSource.Width:F0}x{outgoingSource.Height:F0}) → ({outgoingTarget.X - overlayLeft:F0},{outgoingTarget.Y - overlayTop:F0} {outgoingTarget.Width:F0}x{outgoingTarget.Height:F0})");
				}

				Log.Info("ANIM", $"Overlay: {_overlay.Left:F0},{_overlay.Top:F0} {_overlay.Width:F0}x{_overlay.Height:F0}, placeholders={_overlay.Canvas.Children.Count}");
				_overlay.Show();

				storyboard.Completed += (s, e) =>
				{
					Log.Info("ANIM", "Storyboard completed, removing placeholders");
					if (inPlaceholder != null) _overlay.Canvas.Children.Remove(inPlaceholder);
					if (outPlaceholder != null) _overlay.Canvas.Children.Remove(outPlaceholder);
					if (_overlay.Canvas.Children.Count == 0) _overlay.Hide();
					_isAnimating = false;
					tcs.TrySetResult(true);
				};

				storyboard.Begin();
			}
			catch (Exception ex)
			{
				Log.Info("ANIM", $"Transition failed: {ex.Message}");
				_isAnimating = false;
				_overlay?.Hide();
				tcs.TrySetResult(false);
			}

			return tcs.Task;
		}

		private void EnsureOverlay(Rect bounds)
		{
			if (_overlay == null)
				_overlay = new TransitionOverlayWindow();

			_overlay.Left = bounds.X;
			_overlay.Top = bounds.Y;
			_overlay.Width = bounds.Width;
			_overlay.Height = bounds.Height;
		}

		private static void SetupPlaceholder(Border placeholder, Storyboard storyboard,
			Duration duration, IEasingFunction easing, double overlayLeft, double overlayTop,
			Rect from, Rect to)
		{
			var fromLeft = from.X - overlayLeft;
			var fromTop = from.Y - overlayTop;
			var toLeft = to.X - overlayLeft;
			var toTop = to.Y - overlayTop;

			Canvas.SetLeft(placeholder, fromLeft);
			Canvas.SetTop(placeholder, fromTop);
			placeholder.Width = from.Width;
			placeholder.Height = from.Height;

			storyboard.Children.Add(MakeAnimation(fromLeft, toLeft, duration, easing, Canvas.LeftProperty, placeholder));
			storyboard.Children.Add(MakeAnimation(fromTop, toTop, duration, easing, Canvas.TopProperty, placeholder));
			storyboard.Children.Add(MakeAnimation(from.Width, to.Width, duration, easing, FrameworkElement.WidthProperty, placeholder));
			storyboard.Children.Add(MakeAnimation(from.Height, to.Height, duration, easing, FrameworkElement.HeightProperty, placeholder));
		}

		private static DoubleAnimation MakeAnimation(double from, double to, Duration duration,
			IEasingFunction easing, DependencyProperty property, UIElement target)
		{
			var anim = new DoubleAnimation(from, to, duration) { EasingFunction = easing };
			Storyboard.SetTarget(anim, target);
			Storyboard.SetTargetProperty(anim, new PropertyPath(property));
			return anim;
		}

		public void Dispose()
		{
			if (_overlay != null)
			{
				_overlay.Close();
				_overlay = null;
			}
		}
	}
}
