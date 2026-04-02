// Models/Enums.cs
namespace StudentReminderApp.Models
{
    public enum TrangThaiDangKy
    {
        DaDangKy = 1,
        DaHuy = 2,
        HoanThanh = 3,
        KhongDat = 4
    }

    public enum LoaiDieuKien
    {
        TienQuyet = 1,    // Phải học xong trước
        HocTruoc = 2,     // Có thể đang học cùng kỳ
        SongHanh = 3      // Phải học cùng kỳ
    }

    public enum LoaiHocPhan
    {
        BatBuoc = 1,
        TuChon = 2
    }

    public enum XepLoaiHocLuc
    {
        XuatSac = 1,
        Gioi = 2,
        Kha = 3,
        TrungBinh = 4,
        Yeu = 5
    }
}