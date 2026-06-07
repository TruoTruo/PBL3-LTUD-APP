using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using StudentReminderApp.BLL;

namespace StudentReminderApp.Views.Auth.Components
{
    public partial class ForgotPasswordView : UserControl
    {
        private readonly AccountBLL _bll = new AccountBLL();
        private long _idAcc = 0;
        private bool _otpVerified = false;

        public ForgotPasswordView()
        {
            InitializeComponent();
            Loaded += (s, e) => TxtEmail.Focus();
        }

        private void BtnBackToLogin_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is AuthWindow parent)
                parent.Navigate(new LoginView(parent));
        }

        private void BtnSendOtp_Click(object sender, RoutedEventArgs e)
        {
            TxtStep1Error.Visibility = Visibility.Collapsed;

            string emailInput = TxtEmail.Text.Trim();
            if (string.IsNullOrWhiteSpace(emailInput))
            {
                ShowError(TxtStep1Error, "Vui lòng nhập địa chỉ Email.");
                return;
            }

            Button btn = (Button)sender;
            btn.IsEnabled = false;

            Tuple<bool, string, long, string> result = _bll.SendOtp(emailInput);
            bool success = result.Item1;
            string message = result.Item2;
            long idAcc = result.Item3;
            string maskedEmail = result.Item4;

            btn.IsEnabled = true;

            if (!success)
            {
                ShowError(TxtStep1Error, message);
                return;
            }

            _idAcc = idAcc;
            TxtSubtitle.Text = "Mã OTP đã gửi đến " + maskedEmail + ". Có hiệu lực trong 5 phút.";
            PanelStep1.Visibility = Visibility.Collapsed;
            PanelStep2.Visibility = Visibility.Visible;
            TxtOtp.Focus();
        }

        private void BtnConfirmOtp_Click(object sender, RoutedEventArgs e)
        {
            TxtStep2Error.Visibility = Visibility.Collapsed;

            Tuple<bool, string> result = _bll.ConfirmOtp(_idAcc, TxtOtp.Text.Trim());

            if (!result.Item1)
            {
                ShowError(TxtStep2Error, result.Item2);
                return;
            }

            _otpVerified = true;
            TxtSubtitle.Text = "OTP hợp lệ. Hãy đặt mật khẩu mới cho tài khoản.";
            PanelStep2.Visibility = Visibility.Collapsed;
            PanelStep3.Visibility = Visibility.Visible;
            PwdNew.Focus();
        }

        private void TxtResendOtp_Click(object sender, MouseButtonEventArgs e)
        {
            TxtStep2Error.Visibility = Visibility.Collapsed;
            TxtOtp.Clear();
            Tuple<bool, string, long, string> result = _bll.SendOtp(TxtEmail.Text.Trim());

            if (result.Item1)
            {
                TxtSubtitle.Text = "Đã gửi lại OTP đến " + result.Item4 + ". Có hiệu lực trong 5 phút.";
                ShowSuccess(TxtStep2Error, "✓ Đã gửi lại mã OTP.");
            }
            else ShowError(TxtStep2Error, result.Item2);
        }

        private void BtnBackToStep1_Click(object sender, RoutedEventArgs e)
        {
            TxtStep2Error.Visibility = Visibility.Collapsed;
            PanelStep2.Visibility = Visibility.Collapsed;
            PanelStep1.Visibility = Visibility.Visible;
            TxtSubtitle.Text = "Nhập địa chỉ Email đăng ký tài khoản để nhận mã OTP.";
        }

        private void BtnResetPwd_Click(object sender, RoutedEventArgs e)
        {
            TxtStep3Msg.Visibility = Visibility.Collapsed;

            if (!_otpVerified)
            {
                ShowError(TxtStep3Msg, "Phiên xác thực OTP không hợp lệ. Vui lòng bắt đầu lại.");
                return;
            }

            Tuple<bool, string> result = _bll.ResetPassword(_idAcc, PwdNew.Password, PwdConfirm.Password);

            if (result.Item1)
            {
                ShowSuccess(TxtStep3Msg, result.Item2);
                System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1.5);
                timer.Tick += (s, args) => { timer.Stop(); BtnBackToLogin_Click(sender, e); };
                timer.Start();
            }
            else ShowError(TxtStep3Msg, result.Item2);
        }

        private static void ShowError(TextBlock tb, string msg)
        {
            tb.Text = "⚠ " + msg;
            tb.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            tb.Visibility = Visibility.Visible;
        }

        private static void ShowSuccess(TextBlock tb, string msg)
        {
            tb.Text = msg;
            tb.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            tb.Visibility = Visibility.Visible;
        }
    }
}