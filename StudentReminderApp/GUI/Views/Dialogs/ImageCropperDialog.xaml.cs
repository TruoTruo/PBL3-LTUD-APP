using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;

namespace StudentReminderApp.Views.Dialogs
{
    public partial class ImageCropperDialog : Window
    {
        public string CroppedImagePath { get; private set; }
        
        private string _originalImagePath;
        private BitmapImage _bitmap;
        private Rect _cropRect;
        
        private enum HitType { None, Body, TopLeft, TopRight, BottomRight, BottomLeft }
        private HitType _hitType = HitType.None;
        private bool _isDragging = false;
        private bool _isCropBoxInitialized = false;
        private Point _dragStartPoint;
        private Rect _dragStartRect;

        public ImageCropperDialog(string imagePath)
        {
            InitializeComponent();
            _originalImagePath = imagePath;
            LoadImage();
        }

        private void LoadImage()
        {
            try
            {
                _bitmap = new BitmapImage();
                _bitmap.BeginInit();
                _bitmap.CacheOption = BitmapCacheOption.OnLoad;
                _bitmap.UriSource = new Uri(_originalImagePath, UriKind.Absolute);
                _bitmap.EndInit();
                ImgSource.Source = _bitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không thể tải ảnh: " + ex.Message);
                this.Close();
            }
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ImgSource.Width = ImageCanvas.ActualWidth;
            ImgSource.Height = ImageCanvas.ActualHeight;

            if (!_isCropBoxInitialized && _bitmap != null && ImageCanvas.ActualWidth > 0 && ImageCanvas.ActualHeight > 0)
            {
                InitializeCropBox();
                _isCropBoxInitialized = true;
            }
            else if (_isCropBoxInitialized)
            {
                DrawOverlay();
                UpdateRectAndHandles();
            }
        }

        private void InitializeCropBox()
        {
            double renderRatio = Math.Min(ImageCanvas.ActualWidth / _bitmap.PixelWidth, ImageCanvas.ActualHeight / _bitmap.PixelHeight);
            double renderWidth = _bitmap.PixelWidth * renderRatio;
            double renderHeight = _bitmap.PixelHeight * renderRatio;
            
            double offsetX = (ImageCanvas.ActualWidth - renderWidth) / 2;
            double offsetY = (ImageCanvas.ActualHeight - renderHeight) / 2;

            double size = Math.Min(renderWidth, renderHeight) * 0.5; // 50%
            double x = offsetX + (renderWidth - size) / 2;
            double y = offsetY + (renderHeight - size) / 2;

            _cropRect = new Rect(x, y, size, size);
            SelectionBox.Visibility = Visibility.Visible;
            HandleTL.Visibility = Visibility.Visible;
            HandleTR.Visibility = Visibility.Visible;
            HandleBL.Visibility = Visibility.Visible;
            HandleBR.Visibility = Visibility.Visible;
            
            UpdateRectAndHandles();
            DrawOverlay();
        }

        private void UpdateRectAndHandles()
        {
            Canvas.SetLeft(SelectionBox, _cropRect.X);
            Canvas.SetTop(SelectionBox, _cropRect.Y);
            SelectionBox.Width = _cropRect.Width;
            SelectionBox.Height = _cropRect.Height;

            Canvas.SetLeft(HandleTL, _cropRect.X);
            Canvas.SetTop(HandleTL, _cropRect.Y);

            Canvas.SetLeft(HandleTR, _cropRect.Right);
            Canvas.SetTop(HandleTR, _cropRect.Y);

            Canvas.SetLeft(HandleBL, _cropRect.X);
            Canvas.SetTop(HandleBL, _cropRect.Bottom);

            Canvas.SetLeft(HandleBR, _cropRect.Right);
            Canvas.SetTop(HandleBR, _cropRect.Bottom);
        }

        private HitType SetHitType(Point p)
        {
            double t = 10; // Tolerance

            if (Math.Abs(p.X - _cropRect.X) < t && Math.Abs(p.Y - _cropRect.Y) < t) return HitType.TopLeft;
            if (Math.Abs(p.X - _cropRect.Right) < t && Math.Abs(p.Y - _cropRect.Y) < t) return HitType.TopRight;
            if (Math.Abs(p.X - _cropRect.X) < t && Math.Abs(p.Y - _cropRect.Bottom) < t) return HitType.BottomLeft;
            if (Math.Abs(p.X - _cropRect.Right) < t && Math.Abs(p.Y - _cropRect.Bottom) < t) return HitType.BottomRight;
            
            if (_cropRect.Contains(p)) return HitType.Body;

            return HitType.None;
        }

        private void SetCursor(HitType ht)
        {
            switch (ht)
            {
                case HitType.Body: ImageCanvas.Cursor = Cursors.SizeAll; break;
                case HitType.TopLeft:
                case HitType.BottomRight: ImageCanvas.Cursor = Cursors.SizeNWSE; break;
                case HitType.TopRight:
                case HitType.BottomLeft: ImageCanvas.Cursor = Cursors.SizeNESW; break;
                default: ImageCanvas.Cursor = Cursors.Arrow; break;
            }
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && _isCropBoxInitialized)
            {
                _hitType = SetHitType(e.GetPosition(ImageCanvas));
                if (_hitType == HitType.None) return;

                _isDragging = true;
                _dragStartPoint = e.GetPosition(ImageCanvas);
                _dragStartRect = _cropRect;
                ImageCanvas.CaptureMouse();
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isCropBoxInitialized) return;

            Point p = e.GetPosition(ImageCanvas);

            if (!_isDragging)
            {
                SetCursor(SetHitType(p));
                return;
            }

            double dx = p.X - _dragStartPoint.X;
            double dy = p.Y - _dragStartPoint.Y;

            if (_hitType == HitType.Body)
            {
                _cropRect.X = _dragStartRect.X + dx;
                _cropRect.Y = _dragStartRect.Y + dy;
            }
            else
            {
                double delta = 0;
                
                if (_hitType == HitType.TopLeft)
                {
                    delta = Math.Max(-dx, -dy);
                    _cropRect.X = _dragStartRect.X - delta;
                    _cropRect.Y = _dragStartRect.Y - delta;
                    _cropRect.Width = _dragStartRect.Width + delta;
                    _cropRect.Height = _dragStartRect.Height + delta;
                }
                else if (_hitType == HitType.TopRight)
                {
                    delta = Math.Max(dx, -dy);
                    _cropRect.Y = _dragStartRect.Y - delta;
                    _cropRect.Width = _dragStartRect.Width + delta;
                    _cropRect.Height = _dragStartRect.Height + delta;
                }
                else if (_hitType == HitType.BottomLeft)
                {
                    delta = Math.Max(-dx, dy);
                    _cropRect.X = _dragStartRect.X - delta;
                    _cropRect.Width = _dragStartRect.Width + delta;
                    _cropRect.Height = _dragStartRect.Height + delta;
                }
                else if (_hitType == HitType.BottomRight)
                {
                    delta = Math.Max(dx, dy);
                    _cropRect.Width = _dragStartRect.Width + delta;
                    _cropRect.Height = _dragStartRect.Height + delta;
                }

                // Minimum size
                if (_cropRect.Width < 50)
                {
                    _cropRect.Width = 50;
                    _cropRect.Height = 50;
                }
            }

            UpdateRectAndHandles();
            DrawOverlay();
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ImageCanvas.ReleaseMouseCapture();
            }
        }

        private void DrawOverlay()
        {
            var geometryGroup = new GeometryGroup();
            geometryGroup.Children.Add(new RectangleGeometry(new Rect(0, 0, ImageCanvas.ActualWidth, ImageCanvas.ActualHeight)));
            
            if (_cropRect.Width > 0 && _cropRect.Height > 0)
            {
                var cropGeometry = new RectangleGeometry(_cropRect);
                geometryGroup.Children.Add(cropGeometry);
            }
            
            OverlayPath.Data = geometryGroup;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (_cropRect.Width <= 0 || _cropRect.Height <= 0)
            {
                MessageBox.Show("Vùng chọn bị lỗi.");
                return;
            }

            try
            {
                double renderRatio = Math.Min(ImageCanvas.ActualWidth / _bitmap.PixelWidth, ImageCanvas.ActualHeight / _bitmap.PixelHeight);
                double renderWidth = _bitmap.PixelWidth * renderRatio;
                double renderHeight = _bitmap.PixelHeight * renderRatio;
                
                double offsetX = (ImageCanvas.ActualWidth - renderWidth) / 2;
                double offsetY = (ImageCanvas.ActualHeight - renderHeight) / 2;

                double cropX = (_cropRect.X - offsetX) / renderRatio;
                double cropY = (_cropRect.Y - offsetY) / renderRatio;
                double cropW = _cropRect.Width / renderRatio;
                double cropH = _cropRect.Height / renderRatio;

                // Ràng buộc vào biên ảnh
                cropX = Math.Max(0, cropX);
                cropY = Math.Max(0, cropY);
                if (cropX + cropW > _bitmap.PixelWidth) cropW = _bitmap.PixelWidth - cropX;
                if (cropY + cropH > _bitmap.PixelHeight) cropH = _bitmap.PixelHeight - cropY;

                Int32Rect sourceRect = new Int32Rect((int)cropX, (int)cropY, (int)cropW, (int)cropH);
                if (sourceRect.Width <= 0 || sourceRect.Height <= 0) return;

                CroppedBitmap croppedBitmap = new CroppedBitmap(_bitmap, sourceRect);

                string avatarsDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Avatars");
                if (!System.IO.Directory.Exists(avatarsDir))
                    System.IO.Directory.CreateDirectory(avatarsDir);

                string fileName = $"avatar_crop_{DateTime.Now.Ticks}.jpg";
                CroppedImagePath = System.IO.Path.Combine(avatarsDir, fileName);

                JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(croppedBitmap));
                using (FileStream fs = new FileStream(CroppedImagePath, FileMode.Create))
                {
                    encoder.Save(fs);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi cắt ảnh: " + ex.Message);
            }
        }
    }
}
