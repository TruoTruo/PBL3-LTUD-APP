using System.Collections.Generic;
using StudentReminderApp.DAL;
using StudentReminderApp.Models;
using System.Threading.Tasks;

namespace StudentReminderApp.BLL
{
    public class CourseBLL
    {
        private readonly CourseDAL _dal = new CourseDAL();

        public Task<List<LopHocPhan>> GetAvailableAsync(int hocKy, string namHoc, long idSv) => _dal.GetAvailableAsync(hocKy, namHoc, idSv);

        public async Task<(bool, string)> RegisterAsync(long idSv, long idLopHp)
        {
            bool ok = await _dal.RegisterAsync(idSv, idLopHp);
            if (ok) return (true, "Đăng ký thành công!");
            return (false, "Lỗi: Môn học này có thể đã bị trùng giờ hoặc bạn đã đăng ký rồi.");
        }

        public Task UnregisterAsync(long idSv, long idLopHp) => _dal.UnregisterAsync(idSv, idLopHp);
    }
}