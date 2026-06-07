using System.Collections.Generic;
using StudentReminderApp.DAL;
using StudentReminderApp.Models;
using System.Threading.Tasks;

namespace StudentReminderApp.BLL
{
    public class AdvisorBLL
    {
        private readonly AdvisorDAL _dal = new AdvisorDAL();

        public Task<AdvisorSummary> GetSummaryAsync(long idSv, int hocKy, string namHoc) => _dal.GetSummaryAsync(idSv, hocKy, namHoc);

        public Task<List<LopHocPhan>> GetSuggestedCoursesAsync(long idSv, int hocKy, string namHoc) => _dal.GetSuggestedCoursesAsync(idSv, hocKy, namHoc);

        public Task<List<LopHocPhan>> GetRegisteredCoursesAsync(long idSv, int hocKy, string namHoc) => _dal.GetRegisteredCoursesAsync(idSv, hocKy, namHoc);

        public Task UpdateManualStatsAsync(long idSv, double gpa, int credits) => _dal.UpdateManualStatsAsync(idSv, gpa, credits);
    }
}