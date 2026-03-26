using System.Collections.Generic;
using System.Linq;
using StudentReminderApp.DAL;
using StudentReminderApp.Models;

namespace StudentReminderApp.BLL
{
    public class AdvisorBLL
    {
        private readonly AdvisorDAL _advisorDal = new AdvisorDAL();
        private readonly CourseDAL  _courseDal  = new CourseDAL();

        public AdvisorSummary GetSummary(long idSv, int hocKy, string namHoc)
        {
            return new AdvisorSummary
            {
                TotalAccumulatedCredits   = _advisorDal.GetTotalAccumulatedCredits(idSv),
                RegisteredCreditsThisTerm = _advisorDal.GetRegisteredCredits(idSv, hocKy, namHoc),
                GPA                       = _advisorDal.GetGPA(idSv),
                MaxCreditsAllowed         = 25
            };
        }

        public List<LopHocPhan> GetSuggestedCourses(long idSv, int hocKy, string namHoc)
        {
            var passed    = _advisorDal.GetPassedCourseIds(idSv);
            var available = _courseDal.GetAvailable(hocKy, namHoc, idSv);
            var result    = new List<LopHocPhan>();
            foreach (var lhp in available)
            {
                if (lhp.DaDangKy)                    continue;
                if (passed.Contains(lhp.IdMonHoc))   continue;
                var prereqs = _advisorDal.GetPrerequisites(lhp.IdMonHoc);
                if (prereqs.All(p => passed.Contains(p)))
                    result.Add(lhp);
            }
            return result;
        }

        public List<LopHocPhan> GetRegisteredCourses(long idSv, int hocKy, string namHoc)
            => _courseDal.GetAvailable(hocKy, namHoc, idSv).Where(l => l.DaDangKy).ToList();
    }

    public class AdvisorSummary
    {
        public int    TotalAccumulatedCredits   { get; set; }
        public int    RegisteredCreditsThisTerm { get; set; }
        public double GPA                       { get; set; }
        public int    MaxCreditsAllowed         { get; set; }
        public int    RemainingCredits          => MaxCreditsAllowed - RegisteredCreditsThisTerm;
        public string GPAFormatted              => GPA.ToString("F2");
        public string GPALevel => GPA switch
        {
            >= 3.6 => "Xuất sắc",
            >= 3.2 => "Giỏi",
            >= 2.5 => "Khá",
            >= 2.0 => "Trung bình",
            _      => "Yếu"
        };
    }
}
