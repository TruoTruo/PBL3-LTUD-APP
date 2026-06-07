using System.Collections.Generic;
using System.Linq;
using StudentReminderApp.Helpers;
using StudentReminderApp.Models;

namespace StudentReminderApp.Services
{
    public class ScheduleOptimizerService
    {
        private ScheduleBitmaskHelper _bitmaskHelper = new ScheduleBitmaskHelper();
        private List<List<CourseClass>> _validSchedules = new List<List<CourseClass>>();

        public List<List<CourseClass>> FindAllValidSchedules(List<List<CourseClass>> courseBuckets)
        {
            _validSchedules.Clear();
            Backtrack(courseBuckets, 0, new List<CourseClass>(), 0L);
            return _validSchedules;
        }

        public List<List<CourseClass>> GetOptimizedScheduleOptions(List<List<CourseClass>> courseBuckets, Dictionary<string, string> preferredLecturers = null, int profileIndex = 0)
        {
            var allValid = FindAllValidSchedules(courseBuckets);
            if (allValid.Count == 0)
            {
                return new List<List<CourseClass>>();
            }
            return allValid.OrderByDescending(s => CalculateFitness(s, preferredLecturers, profileIndex)).Take(3).ToList();
        }

        private void Backtrack(List<List<CourseClass>> courseBuckets, int bucketIndex, List<CourseClass> currentCombination, long currentMask)
        {
            if (bucketIndex == courseBuckets.Count)
            {
                _validSchedules.Add(new List<CourseClass>(currentCombination));
                return;
            }

            foreach (var courseClass in courseBuckets[bucketIndex])
            {
                long classMask = _bitmaskHelper.ConvertScheduleToBitmask(courseClass.sessions);

                if (!_bitmaskHelper.IsConflict(currentMask, classMask))
                {
                    currentCombination.Add(courseClass);
                    long newMask = _bitmaskHelper.MergeSchedules(currentMask, classMask);
                    Backtrack(courseBuckets, bucketIndex + 1, currentCombination, newMask);
                    currentCombination.RemoveAt(currentCombination.Count - 1);
                }
            }
        }

        public double CalculateFitness(List<CourseClass> schedule, Dictionary<string, string> preferredLecturers = null, int profileIndex = 0)
        {
            double score = 0;
            long fullMask = 0;
            foreach (var courseClass in schedule)
            {
                fullMask = _bitmaskHelper.MergeSchedules(fullMask, _bitmaskHelper.ConvertScheduleToBitmask(courseClass.sessions));
                
                // Tiêu chí 1: Săn giảng viên
                if (preferredLecturers != null && preferredLecturers.ContainsKey(courseClass.courseId))
                {
                    if (courseClass.LecturerName != null && courseClass.LecturerName.Contains(preferredLecturers[courseClass.courseId]))
                    {
                        score += 100.0; // Điểm cực cao để ưu tiên hàng đầu
                    }
                }
            }

            // Thiết lập trọng số theo Profile
            double daysOffWeight = 20.0;
            double morningWeight = 0;
            double afternoonWeight = 0;
            double earlyMorningPenalty = 0;

            if (profileIndex == 1) // Cú đêm
            {
                earlyMorningPenalty = -10.0;
                afternoonWeight = 5.0;
            }
            else if (profileIndex == 2) // Chim sớm
            {
                morningWeight = 5.0;
                afternoonWeight = -5.0;
            }
            else if (profileIndex == 3) // Tín đồ về quê
            {
                daysOffWeight = 50.0;
            }

            int daysOff = 0;
            int non99Groups = 0;
            int halfDays = 0;

            for (int i = 0; i < 6; i++) // Từ Thứ 2 đến Thứ 7
            {
                long dayMask = (fullMask >> (i * 10)) & 0x3FF;
                if (dayMask == 0)
                {
                    daysOff++;
                }
                else
                {
                    int firstBit = -1;
                    int lastBit = -1;
                    int classCount = 0;
                    for (int j = 0; j < 10; j++)
                    {
                        if ((dayMask & (1L << j)) != 0)
                        {
                            if (firstBit == -1) firstBit = j;
                            lastBit = j;
                            classCount++;

                            // Tiêu chí 2: Tránh tiết hiểm độc (Tiết 5 là bit 4, Tiết 10 là bit 9)
                            if (j == 4 || j == 9) score -= 5.0;

                            // Profile Cú đêm: Phạt tiết 1, 2
                            if (j == 0 || j == 1) score += earlyMorningPenalty;
                        }
                    }

                    // Tiêu chí 3: Quá tải trong ngày
                    if (classCount > 6) score -= 15.0;

                    // Tiêu chí 4: Phạt lũy tiến khoảng trống
                    int consecutiveGaps = 0;
                    for (int j = firstBit + 1; j < lastBit; j++)
                    {
                        if ((dayMask & (1L << j)) == 0)
                        {
                            consecutiveGaps++;
                            if (consecutiveGaps == 1) score -= 2.0;
                            else if (consecutiveGaps == 2) score -= 4.0; // Tổng -6
                            else score -= 9.0; // Tổng -15
                        }
                        else
                        {
                            consecutiveGaps = 0;
                        }
                    }
                    
                    bool hasMorning = (dayMask & 0x1F) != 0;
                    bool hasAfternoon = (dayMask & 0x3E0) != 0;
                    if (hasMorning ^ hasAfternoon) halfDays++;
                    
                    if (hasMorning) score += morningWeight;
                    if (hasAfternoon) score += afternoonWeight;
                }
            }

            // Tiêu chí 5: Di chuyển giữa các dãy nhà
            for (int i = 0; i < 6; i++) // Thứ 2 đến Thứ 7
            {
                var dayClasses = new List<Session>();
                foreach (var c in schedule)
                {
                    foreach (var s in c.sessions)
                    {
                        if (s.dayOfWeek == i + 2) dayClasses.Add(s);
                    }
                }
                dayClasses = dayClasses.OrderBy(s => s.lessons.Count > 0 ? s.lessons.Min() : 0).ToList();
                for (int k = 0; k < dayClasses.Count - 1; k++)
                {
                    var c1 = dayClasses[k];
                    var c2 = dayClasses[k + 1];
                    if (c1.lessons.Count > 0 && c2.lessons.Count > 0 && c1.lessons.Max() + 1 == c2.lessons.Min())
                    {
                        if (!string.IsNullOrWhiteSpace(c1.room) && !string.IsNullOrWhiteSpace(c2.room))
                        {
                            char building1 = c1.room.Trim()[0];
                            char building2 = c2.room.Trim()[0];
                            if (char.IsLetter(building1) && char.IsLetter(building2) && building1 != building2)
                            {
                                score -= 10.0;
                            }
                        }
                    }
                }
            }

            foreach (var courseClass in schedule)
            {
                if (string.IsNullOrEmpty(courseClass.group) || !courseClass.group.StartsWith("99")) non99Groups++;
            }

            score += (daysOff * daysOffWeight) + (halfDays * 5.0) - (non99Groups * 5.0);
            return score;
        }
    }
}