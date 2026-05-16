using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using StudentReminderApp.BLL;

namespace StudentReminderApp.Views.Auth
{
    public partial class ForgotPasswordWindow : Window
    {
        private readonly AccountBLL _bll = new AccountBLL();

        private long _idAcc      = 0;
        private bool _otpVerified = false;

        public ForgotPasswordWindow() => InitializeComponent();

        // ════════════════════════════════════════════════════════════
        // BƯỚC 1 — Nhập Email → Gửi OTP
        // ════════════════════════════════════════════════════════════
        private void BtnSendOtp_Click(object sender, RoutedEventArgs e)
        {
            TxtStep1Error.Visibility = Visibility.Collapsed;

            string emailInput = TxtEmail.Text.Trim();
            if (string.IsNullOrWhiteSpace(emailInput))
            {
                ShowError(TxtStep1Error, "Vui lòng nhập địa chỉ Email.");
                return;
            }

            System.Windows.Controls.Button btn =
                (System.Windows.Controls.Button)sender;
            btn.IsEnabled = false;
            btn.Content   = "⏳ Đang gửi...";

            // Dùng Tuple tường minh — tránh CS8130
            Tuple<bool, string, long, string> result = _bll.SendOtp(emailInput);
            bool   success     = result.Item1;
            string message     = result.Item2;
            long   idAcc       = result.Item3;
            string maskedEmail = result.Item4;

            btn.IsEnabled = true;
            btn.Content   = "Gửi mã OTP →";

            if (!success)
            {
                ShowError(TxtStep1Error, message);
                return;
            }

            _idAcc = idAcc;

            TxtSubtitle.Text      = "Mã OTP đã gửi đến " + maskedEmail + ". Có hiệu lực trong 5 phút.";
            PanelStep1.Visibility = Visibility.Collapsed;
            PanelStep2.Visibility = Visibility.Visible;
            TxtOtp.Focus();
        }

        // ════════════════════════════════════════════════════════════
        // BƯỚC 2 — Nhập OTP → Xác nhận
        // ════════════════════════════════════════════════════════════
        private void BtnConfirmOtp_Click(object sender, RoutedEventArgs e)
        {
            TxtStep2Error.Visibility = Visibility.Collapsed;

            Tuple<bool, string> result = _bll.ConfirmOtp(_idAcc, TxtOtp.Text.Trim());
            bool   success = result.Item1;
            string message = result.Item2;

            if (!success)
            {
                ShowError(TxtStep2Error, message);
                return;
            }

            _otpVerified = true;

            TxtSubtitle.Text      = "OTP hợp lệ. Hãy đặt mật khẩu mới cho tài khoản.";
            PanelStep2.Visibility = Visibility.Collapsed;
            PanelStep3.Visibility = Visibility.Visible;
            PwdNew.Focus();
        }

        // ── Gửi lại OTP ──────────────────────────────────────────
        private void TxtResendOtp_Click(object sender, MouseButtonEventArgs e)
        {
            TxtStep2Error.Visibility = Visibility.Collapsed;
            TxtOtp.Clear();

            Tuple<bool, string, long, string> result = _bll.SendOtp(TxtEmail.Text.Trim());
            bool   success     = result.Item1;
            string message     = result.Item2;
            string maskedEmail = result.Item4;

            if (success)
            {
                TxtSubtitle.Text = "Đã gửi lại OTP đến " + maskedEmail + ". Có hiệu lực trong 5 phút.";
                ShowSuccess(TxtStep2Error, "✓ Đã gửi lại mã OTP.");
            }
            else
                ShowError(TxtStep2Error, message);
        }

        // ── Quay lại Bước 1 ──────────────────────────────────────
        private void BtnBackToStep1_Click(object sender, RoutedEventArgs e)
        {
            TxtStep2Error.Visibility = Visibility.Collapsed;
            PanelStep2.Visibility    = Visibility.Collapsed;
            PanelStep1.Visibility    = Visibility.Visible;
            TxtSubtitle.Text         = "Nhập địa chỉ Email đăng ký tài khoản để nhận mã OTP.";
        }

        // ════════════════════════════════════════════════════════════
        // BƯỚC 3 — Đặt lại mật khẩu
        // ════════════════════════════════════════════════════════════
        private void BtnResetPwd_Click(object sender, RoutedEventArgs e)
        {
            TxtStep3Msg.Visibility = Visibility.Collapsed;

            if (!_otpVerified)
            {
                ShowError(TxtStep3Msg, "Phiên xác thực OTP không hợp lệ. Vui lòng bắt đầu lại.");
                return;
            }

            Tuple<bool, string> result = _bll.ResetPassword(
                _idAcc, PwdNew.Password, PwdConfirm.Password);
            bool   success = result.Item1;
            string message = result.Item2;

            if (success)
            {
                ShowSuccess(TxtStep3Msg, message);

                System.Windows.Threading.DispatcherTimer timer =
                    new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1.5);
                timer.Tick += (s, args) => { timer.Stop(); Close(); };
                timer.Start();
            }
            else
                ShowError(TxtStep3Msg, message);
        }

        // ── Helpers ───────────────────────────────────────────────
        private static void ShowError(System.Windows.Controls.TextBlock tb, string msg)
        {
            tb.Text       = "⚠ " + msg;
            tb.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            tb.Visibility = Visibility.Visible;
        }

        private static void ShowSuccess(System.Windows.Controls.TextBlock tb, string msg)
        {
            tb.Text       = msg;
            tb.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            tb.Visibility = Visibility.Visible;
        }
    }
}