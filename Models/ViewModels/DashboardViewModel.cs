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

    public class ChiPhiTheoThangDto
    {
        public string ThangNam { get; set; } = string.Empty; // e.g. "T03/2026"
        public decimal TongTien { get; set; }
        public int SoPhieuNhap { get; set; }
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

        // Cảnh báo tồn kho
        public int SoMatHangCanhBao { get; set; }
        public List<TonKho> DanhSachCanhBaoTonKho { get; set; } = new List<TonKho>();

        // Dữ liệu thống kê biểu đồ động từ SQL Server
        public List<DanhMucThongKeDto> ThongKeDanhMuc { get; set; } = new List<DanhMucThongKeDto>();
        public List<NhaCungCapDiemDto> ThongKeNhaCungCapAHP { get; set; } = new List<NhaCungCapDiemDto>();
        public List<ChiPhiTheoThangDto> ThongKeChiPhiTheoThang { get; set; } = new List<ChiPhiTheoThangDto>();
    }
}
