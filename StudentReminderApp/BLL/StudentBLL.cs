using System;
using System.Collections.Generic;
using StudentReminderApp.DAL;
using StudentReminderApp.Helpers;
using StudentReminderApp.Models;

namespace StudentReminderApp.BLL
{
    public class StudentBLL
    {
        private readonly StudentDAL _dal = new StudentDAL();

        public List<StudentModel> GetAllStudents()
        {
            try { return _dal.GetAllStudents(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("StudentBLL.GetAllStudents: " + ex.Message);
                return new List<StudentModel>();
            }
        }

        public List<(long IdLop, string TenLop)> GetAllClasses()
        {
            try { return _dal.GetAllClasses(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("StudentBLL.GetAllClasses: " + ex.Message);
                return new List<(long, string)>();
            }
        }

        /// <summary>
        /// Cập nhật lớp cho sinh viên.
        /// Gọi từ cả Admin (StudentViewModel) lẫn sinh viên tự cập nhật (ProfilePage).
        /// </summary>
        public bool UpdateStudentClass(long idAcc, long? idLop)
        {
            if (idAcc <= 0) return false;
            try { return _dal.UpdateStudentClass(idAcc, idLop); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("StudentBLL.UpdateStudentClass: " + ex.Message);
                return false;
            }
        }

        public bool BanStudent(long idAcc, DateTime? lockUntil)
        {
            if (!SessionManager.IsAdmin || idAcc <= 0) return false;
            try   { return _dal.BanStudent(idAcc, lockUntil); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("StudentBLL.BanStudent: " + ex.Message);
                return false;
            }
        }

        public bool UnbanStudent(long idAcc)
        {
            if (!SessionManager.IsAdmin || idAcc <= 0) return false;
            try   { return _dal.UnbanStudent(idAcc); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("StudentBLL.UnbanStudent: " + ex.Message);
                return false;
            }
        }

        public bool VerifyStudent(long idAcc)
        {
            if (!SessionManager.IsAdmin || idAcc <= 0) return false;
            try   { return _dal.VerifyStudent(idAcc); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("StudentBLL.VerifyStudent: " + ex.Message);
                return false;
            }
        }
    }
}
