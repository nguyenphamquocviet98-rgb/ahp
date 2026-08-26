using System.Collections.Generic;
using code.Models;

namespace code.Models.ViewModels
{
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
    }
}
