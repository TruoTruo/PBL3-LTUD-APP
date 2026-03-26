using System.Collections.Generic;
using StudentReminderApp.DAL;
using StudentReminderApp.Models;

namespace StudentReminderApp.BLL
{
    public class CourseBLL
    {
        private readonly CourseDAL _dal = new CourseDAL();

        public List<LopHocPhan> GetAvailable(int hocKy, string namHoc, long idSv)
            => _dal.GetAvailable(hocKy, namHoc, idSv);

        public (bool ok, string msg) Register(long idSv, long idLopHp)
        {
            try
            {
                _dal.Register(idSv, idLopHp);
                return (true, "Đăng ký thành công!");
            }
            catch (System.Exception ex)
            {
                if (ex.Message.Contains("idx_unique_dki"))
                    return (false, "Bạn đã đăng ký lớp học phần này rồi.");
                return (false, "Lỗi: " + ex.Message);
            }
        }

        public void Unregister(long idSv, long idLopHp) => _dal.Unregister(idSv, idLopHp);
    }
}
