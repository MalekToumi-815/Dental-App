using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Dental_App.Views
{
    public partial class OrdonnanceTemplateDialogView : UserControl
    {
        private bool _isDragging = false;
        private Point _clickPosition;

        public OrdonnanceTemplateDialogView()
        {
            InitializeComponent();
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is ViewModels.OrdonnanceTemplateDialogViewModel vm && vm.HasImage)
            {
                // Only allow dragging if we didn't click on one of the resize thumbs
                if (e.OriginalSource is Thumb)
                    return;

                _isDragging = true;
                _clickPosition = e.GetPosition(OverlayCanvas);

                // Instantly teleport the box to where they clicked just to make it easy
                vm.X = _clickPosition.X - (vm.BoxWidth / 2);
                vm.Y = _clickPosition.Y - (vm.BoxHeight / 2);

                OverlayCanvas.CaptureMouse();
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && DataContext is ViewModels.OrdonnanceTemplateDialogViewModel vm)
            {
                var mousePos = e.GetPosition(OverlayCanvas);

                // Optional logic: constrain within canvas boundaries
                double newX = mousePos.X - (vm.BoxWidth / 2);
                double newY = mousePos.Y - (vm.BoxHeight / 2);

                if (newX < 0) newX = 0;
                if (newY < 0) newY = 0;

                if (newX + vm.BoxWidth > OverlayCanvas.ActualWidth)
                    newX = OverlayCanvas.ActualWidth - vm.BoxWidth;

                if (newY + vm.BoxHeight > OverlayCanvas.ActualHeight)
                    newY = OverlayCanvas.ActualHeight - vm.BoxHeight;

                vm.X = newX;
                vm.Y = newY;
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                OverlayCanvas.ReleaseMouseCapture();
            }
        }

        // --- Corner Resizing Logic ---

        private void Thumb_DragDelta_TopLeft(object sender, DragDeltaEventArgs e)
        {
            if (DataContext is ViewModels.OrdonnanceTemplateDialogViewModel vm)
            {
                // Move X/Y and expand/shrink Width/Height backwards
                double newW = Math.Max(50, vm.BoxWidth - e.HorizontalChange);
                if (newW != 50) vm.X += e.HorizontalChange;
                vm.BoxWidth = newW;
                
                double newH = Math.Max(50, vm.BoxHeight - e.VerticalChange);
                if (newH != 50) vm.Y += e.VerticalChange;
                vm.BoxHeight = newH;
            }
        }

        private void Thumb_DragDelta_TopRight(object sender, DragDeltaEventArgs e)
        {
            if (DataContext is ViewModels.OrdonnanceTemplateDialogViewModel vm)
            {
                vm.BoxWidth = Math.Max(50, vm.BoxWidth + e.HorizontalChange);
                
                double newH = Math.Max(50, vm.BoxHeight - e.VerticalChange);
                if (newH != 50) vm.Y += e.VerticalChange;
                vm.BoxHeight = newH;
            }
        }

        private void Thumb_DragDelta_BottomLeft(object sender, DragDeltaEventArgs e)
        {
            if (DataContext is ViewModels.OrdonnanceTemplateDialogViewModel vm)
            {
                double newW = Math.Max(50, vm.BoxWidth - e.HorizontalChange);
                if (newW != 50) vm.X += e.HorizontalChange;
                vm.BoxWidth = newW;
                
                vm.BoxHeight = Math.Max(50, vm.BoxHeight + e.VerticalChange);
            }
        }

        private void Thumb_DragDelta_BottomRight(object sender, DragDeltaEventArgs e)
        {
            if (DataContext is ViewModels.OrdonnanceTemplateDialogViewModel vm)
            {
                vm.BoxWidth = Math.Max(50, vm.BoxWidth + e.HorizontalChange);
                vm.BoxHeight = Math.Max(50, vm.BoxHeight + e.VerticalChange);
            }
        }
    }
}
