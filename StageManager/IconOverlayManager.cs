using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using StageManager.Model;

namespace StageManager
{
    internal sealed class IconOverlayManager : IDisposable
    {
        private const double IconSize = 30;
        private const double IconGap = 4;
        private const double OverlapOffset = -14;
        private const double SmallSceneLeftShift = -8;
        private const double BottomOverlap = 15;
        private const double SceneBottomMargin = 28;
        private const int CompactThreshold = 2;

        private static readonly DropShadowEffect SharedShadow = CreateFrozenShadow();

        private IconOverlayWindow? _overlay;

        public bool Enabled { get; set; } = true;

        public void UpdateIcons(
            IReadOnlyList<SceneModel> visibleScenes,
            Func<SceneModel, Rect> getSceneBounds,
            Rect workArea,
            double xOffset = 0)
        {
            if (!Enabled || workArea == Rect.Empty)
                return;

            EnsureOverlay(workArea);

            _overlay!.Canvas.Children.Clear();

            foreach (var scene in visibleScenes)
            {
                var bounds = getSceneBounds(scene);
                if (bounds == Rect.Empty || scene.Windows.Count == 0)
                    continue;

                var panel = CreateIconPanel(scene);
                if (panel.Children.Count == 0)
                    continue;

                PositionPanel(panel, bounds, scene.Windows.Count, xOffset);
                _overlay.Canvas.Children.Add(panel);
            }

            if (_overlay.Canvas.Children.Count == 0)
                _overlay.Hide();
            else
                _overlay.Show();
        }

        public void Show(Rect workArea)
        {
            if (workArea == Rect.Empty)
                return;

            EnsureOverlay(workArea);
            _overlay!.Show();
        }

        public void Hide()
        {
            _overlay?.Hide();
        }

        public void SlideIn(double offsetX, TimeSpan duration, IEasingFunction easing, Action? onCompleted = null)
        {
            if (_overlay == null) return;
            var transform = new TranslateTransform(offsetX, 0);
            _overlay.Canvas.RenderTransform = transform;
            var anim = new DoubleAnimation(offsetX, 0, new Duration(duration)) { EasingFunction = easing };
            anim.Completed += (_, _) =>
            {
                _overlay.Canvas.RenderTransform = Transform.Identity;
                onCompleted?.Invoke();
            };
            transform.BeginAnimation(TranslateTransform.XProperty, anim);
        }

        public void SlideOut(double offsetX, TimeSpan duration, IEasingFunction easing)
        {
            if (_overlay == null) return;
            var transform = _overlay.Canvas.RenderTransform as TranslateTransform ?? new TranslateTransform();
            _overlay.Canvas.RenderTransform = transform;
            var anim = new DoubleAnimation(0, offsetX, new Duration(duration)) { EasingFunction = easing };
            anim.Completed += (_, _) => Hide();
            transform.BeginAnimation(TranslateTransform.XProperty, anim);
        }

        public void Dispose()
        {
            _overlay?.Close();
            _overlay = null;
        }

        private void EnsureOverlay(Rect bounds)
        {
            if (_overlay == null)
                _overlay = new IconOverlayWindow();

            _overlay.Left = bounds.X;
            _overlay.Top = bounds.Y;
            _overlay.Width = bounds.Width;
            _overlay.Height = bounds.Height;
        }

        private static StackPanel CreateIconPanel(SceneModel scene)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            var count = scene.Windows.Count;

            for (int i = 0; i < count; i++)
            {
                var icon = scene.Windows[i].Icon;
                if (icon == null)
                    continue;

                var image = new Image
                {
                    Height = IconSize,
                    Source = icon,
                };

                var border = new Border
                {
                    Child = image,
                    Effect = SharedShadow,
                    Margin = GetIconMargin(i, count),
                };

                panel.Children.Add(border);
            }

            return panel;
        }

        private static Thickness GetIconMargin(int index, int count)
        {
            if (count <= CompactThreshold)
                return new Thickness(0, 0, IconGap, 0);

            return index == 0
                ? new Thickness(0)
                : new Thickness(OverlapOffset, 0, 0, 0);
        }

        private void PositionPanel(StackPanel panel, Rect thumbnailBounds, int windowCount, double extraXOffset = 0)
        {
            var overlayLeft = _overlay!.Left;
            var overlayTop = _overlay.Top;

            var xOffset = windowCount <= CompactThreshold ? SmallSceneLeftShift : 0;
            var left = thumbnailBounds.X - overlayLeft + xOffset + extraXOffset;
            var contentBottom = thumbnailBounds.Y + thumbnailBounds.Height - SceneBottomMargin;
            var top = contentBottom - overlayTop - BottomOverlap;

            Canvas.SetLeft(panel, left);
            Canvas.SetTop(panel, top);
        }

        private static DropShadowEffect CreateFrozenShadow()
        {
            var effect = new DropShadowEffect { BlurRadius = 20 };
            effect.Freeze();
            return effect;
        }
    }
}
