using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using Microsoft.Win32;
using System.Windows.Controls;
using StudentReminderApp.ViewModels;
using StudentReminderApp.Helpers;

namespace StudentReminderApp.Views.Dialogs
{
    public partial class CreatePostDialog : Window
    {
        public CreatePostDialog()
        {
            InitializeComponent();

            this.Loaded += (s, e) =>
            {
                if (this.DataContext is ForumViewModel vm)
                {
                    vm.CloseAction = new Action(this.Close);
                    vm.NewContent = string.Empty;
                    if (vm.SelectedFiles != null) vm.SelectedFiles.Clear();

                    CommandManager.InvalidateRequerySuggested();
                }
                LoadUserData();
            };
        }

        private void LoadUserData()
        {
            if (SessionManager.CurrentUser != null)
            {
                if (TxtUserName != null) TxtUserName.Text = SessionManager.CurrentUser.HoTen;
                if (TxtPlaceholder != null) TxtPlaceholder.Text = $"{SessionManager.CurrentUser.HoTen} ơi, bạn đang nghĩ gì thế?";
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();

        private void BtnAnonymous_Toggle(object sender, RoutedEventArgs e)
        {
            if (TxtUserName == null || BtnAnonymous == null) return;
            TxtUserName.Text = (BtnAnonymous.IsChecked == true) ? "Thành viên ẩn danh" : (SessionManager.CurrentUser?.HoTen ?? "Người dùng");
        }

        private void SelectFile_Click(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Multiselect = true };
            openFileDialog.Filter = "Hình ảnh|*.jpg;*.jpeg;*.png|Tất cả các file|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                if (this.DataContext is ForumViewModel vm)
                {
                    foreach (string filePath in openFileDialog.FileNames)
                    {
                        vm.AddFileToList(filePath);
                    }
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private void ToggleColorPicker_Click(object sender, MouseButtonEventArgs e)
        {
            if (ColorPickerPanel != null)
                ColorPickerPanel.Visibility = (ColorPickerPanel.Visibility == Visibility.Visible) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void SelectColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Background != null)
            {
                PostBackgroundBorder.Background = btn.Background;

                if (this.DataContext is ForumViewModel vm)
                {
                    if (btn.Tag?.ToString() == "Normal")
                    {
                        TxtPostContent.Foreground = Brushes.Black;
                        TxtPostContent.FontSize = 18;
                        TxtPostContent.FontWeight = FontWeights.Normal;
                        TxtPostContent.TextAlignment = TextAlignment.Left;
                        TxtPostContent.VerticalContentAlignment = VerticalAlignment.Top;
                        vm.SelectedColor = "Transparent";
                    }
                    else
                    {
                        TxtPostContent.Foreground = Brushes.White;
                        TxtPostContent.FontSize = 24;
                        TxtPostContent.FontWeight = FontWeights.Bold;
                        TxtPostContent.TextAlignment = TextAlignment.Center;
                        TxtPostContent.VerticalContentAlignment = VerticalAlignment.Center;
                        vm.SelectedColor = btn.Background.ToString();
                    }
                }
            }
        }

        private void ToggleEmojiPopup_Click(object sender, MouseButtonEventArgs e)
        {
            EmojiPopup.IsOpen = !EmojiPopup.IsOpen;

            e.Handled = true;
        }

        private void EmojiListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EmojiListBox.SelectedItem is TextBlock selectedEmoji)
            {
                int caretIndex = TxtPostContent.CaretIndex;
                string emojiText = selectedEmoji.Text;

                TxtPostContent.Text = TxtPostContent.Text.Insert(caretIndex, emojiText);

                TxtPostContent.CaretIndex = caretIndex + emojiText.Length;

                TxtPostContent.Focus();

                EmojiPopup.IsOpen = false;
                EmojiListBox.SelectedIndex = -1;
            }
        }
    }
}
