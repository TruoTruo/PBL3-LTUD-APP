using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LTUDAPI.Data;
using LTUDAPI.Models;
using LTUDAPI.DTOs;

namespace LTUDAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReminderController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReminderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Lấy danh sách cấu hình nhắc lịch của tất cả người dùng (Admin)
        // GET: api/Reminder
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ReminderConfig>>> GetReminders()
        {
            return await _context.ReminderConfigs.ToListAsync();
        }

        // 2. Lấy cấu hình nhắc lịch của 1 tài khoản cụ thể
        // GET: api/Reminder/5
        [HttpGet("{idAcc}")]
        public async Task<ActionResult<ReminderConfig>> GetReminderByAccount(long idAcc)
        {
            var config = await _context.ReminderConfigs
                .FirstOrDefaultAsync(r => r.IdAcc == idAcc);

            if (config == null) 
            {
                return NotFound(new { message = "Không tìm thấy cấu hình cho tài khoản này." });
            }
            return config;
        }

        // 3. Thêm hoặc cập nhật cấu hình nhắc lịch
        // POST: api/Reminder
        [HttpPost]
        public async Task<ActionResult> PostReminder(ReminderRequest request)
        {
            // Kiểm tra tài khoản có tồn tại không trước khi cài đặt nhắc lịch
            var accountExists = await _context.Accounts.AnyAsync(a => a.IdAcc == request.IdAcc);
            if (!accountExists)
            {
                return BadRequest(new { message = "Tài khoản không tồn tại." });
            }

            // Kiểm tra xem đã có cấu hình chưa
            var existingConfig = await _context.ReminderConfigs
                .FirstOrDefaultAsync(r => r.IdAcc == request.IdAcc);

            if (existingConfig != null)
            {
                // Nếu có rồi thì cập nhật
                existingConfig.MinsBefore = request.MinsBefore;
                existingConfig.IsEnabled = request.IsEnabled;
                existingConfig.Channel = request.Channel;
                _context.ReminderConfigs.Update(existingConfig);
            }
            else
            {
                // Nếu chưa có thì tạo mới
                var newConfig = new ReminderConfig
                {
                    IdAcc = request.IdAcc,
                    MinsBefore = request.MinsBefore,
                    IsEnabled = request.IsEnabled,
                    Channel = request.Channel
                };
                _context.ReminderConfigs.Add(newConfig);
            }

            // Ghi log vào bảng USER_LOG
            var log = new UserLog
            {
                IdAcc = request.IdAcc,
                HanhDong = $"Cài đặt nhắc lịch: {request.MinsBefore} phút qua {request.Channel}",
                ThoiGian = DateTime.Now,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
            };
            _context.UserLogs.Add(log);

            try
            {
                await _context.SaveChangesAsync();
                return Ok(new { message = "Lưu cấu hình thành công!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // 4. Xóa cấu hình nhắc lịch
        // DELETE: api/Reminder/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReminder(long id)
        {
            var config = await _context.ReminderConfigs.FindAsync(id);
            if (config == null) return NotFound();

            _context.ReminderConfigs.Remove(config);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa cấu hình thành công." });
        }
    }
}