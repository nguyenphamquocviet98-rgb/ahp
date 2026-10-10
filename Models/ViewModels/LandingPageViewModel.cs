using System.Collections.Generic;
using code.Models;

namespace code.Models.ViewModels
{
    public class DanhMucLandingDto
    {
        public int MaDanhMuc { get; set; }
        public string TenDanhMuc { get; set; } = string.Empty;
        public int SoLuongSanPham { get; set; }
        public string IconClass { get; set; } = "bi-box";
    }

    public class NhaPhanPhoiLandingDto
    {
        public int MaDoanhNghiep { get; set; }
        public string TenDoanhNghiep { get; set; } = string.Empty;
        public string? DiaChi { get; set; }
        public string? SoDienThoai { get; set; }
        public decimal DiemUyTin { get; set; }
        public bool LaGianHangMall { get; set; }
        public int SoSanPhamChaoBan { get; set; }
        public string? Logo { get; set; }
        public string? MoTa { get; set; }
        public bool CoDeXuatAHP { get; set; }
    }

    public class SanPhamB2BLandingDto
    {
        public int MaSanPham { get; set; }
        public int? MaTonKhoNPP { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public string? TenDanhMuc { get; set; }
        public string? DonViTinh { get; set; }
        public decimal GiaSi { get; set; }
        public decimal GiaGoc { get; set; }
        public string TenNhaCungCap { get; set; } = string.Empty;
        public int MaDoanhNghiep { get; set; }
        public bool LaMall { get; set; }
        public int ThoiGianGiaoHang { get; set; } = 1;
        public int SoLuongCoSan { get; set; }
        public string? AnhSanPham { get; set; }
    }

    public class DemoAccountDto
    {
        public int MaNguoiDung { get; set; }
        public string TenDangNhap { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public string VaiTro { get; set; } = string.Empty;
        public string TenDoanhNghiep { get; set; } = string.Empty;
        public bool LaMall { get; set; }
    }

    public class LandingPageViewModel
    {
        public List<DanhMucLandingDto> DanhSachDanhMuc { get; set; } = new List<DanhMucLandingDto>();
        public List<NhaPhanPhoiLandingDto> DanhSachNhaPhanPhoi { get; set; } = new List<NhaPhanPhoiLandingDto>();
        public List<SanPhamB2BLandingDto> DanhSachSanPhamHot { get; set; } = new List<SanPhamB2BLandingDto>();
        public List<DemoAccountDto> TaiKhoanDemo { get; set; } = new List<DemoAccountDto>();

        // Thống kê sàn
        public int TongNhaPhanPhoi { get; set; }
        public int TongSanPham { get; set; }
        public int TongGiaoDich { get; set; }
        public decimal TiLeToiUuAHP { get; set; } = 99.4m;
    }
}
