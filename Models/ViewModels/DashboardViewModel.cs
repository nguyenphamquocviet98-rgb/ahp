using System.Collections.Generic;
using code.Models;

namespace code.Models.ViewModels
{
    public class DanhMucThongKeDto
    {
        public string TenDanhMuc { get; set; } = string.Empty;
        public int SoLuongSanPham { get; set; }
    }

    public class NhaCungCapDiemDto
    {
        public string TenNhaCungCap { get; set; } = string.Empty;
        public decimal DiemTong { get; set; }
        public int XepHang { get; set; }
        public bool DeXuat { get; set; }
    }

    public class DashboardViewModel
    {
        public int TongSanPham { get; set; }
        public int TongNhaCungCap { get; set; }
        public int TongPhieuNhap { get; set; }
        public decimal TongGiaTriNhap { get; set; }
        public int TongSoLuongTon { get; set; }

        public KetQuaXepHang? NhaCungCapDeXuatGanNhat { get; set; }
        public List<PhieuNhap> PhieuNhapMoiNhat { get; set; } = new List<PhieuNhap>();
        public List<TonKho> DanhSachTonKho { get; set; } = new List<TonKho>();

        // Dữ liệu thống kê biểu đồ động lấy 100% từ cơ sở dữ liệu SQL
        public List<DanhMucThongKeDto> ThongKeDanhMuc { get; set; } = new List<DanhMucThongKeDto>();
        public List<NhaCungCapDiemDto> ThongKeNhaCungCapAHP { get; set; } = new List<NhaCungCapDiemDto>();
    }
}
