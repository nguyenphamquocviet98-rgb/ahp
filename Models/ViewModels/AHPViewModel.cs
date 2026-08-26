using System.Collections.Generic;
using code.Models;
using code.Services;

namespace code.Models.ViewModels
{
    public class AHPPeriodListViewModel
    {
        public List<KyDanhGia> KyDanhGias { get; set; } = new List<KyDanhGia>();
    }

    public class MaTranSoSanhInputModel
    {
        public int MaKyDanhGia { get; set; }
        public string TenKyDanhGia { get; set; } = string.Empty;
        public List<TieuChi> DanhSachTieuChi { get; set; } = new List<TieuChi>();

        // MaTran[i, j]
        public double[][] MaTran { get; set; } = new double[0][];

        // Kết quả AHP (nếu đã tính)
        public AHPResult? KetQuaAHP { get; set; }
        public List<TrongSoTieuChi> TrongSos { get; set; } = new List<TrongSoTieuChi>();
    }

    public class ChamDiemNCCInputModel
    {
        public int MaKyDanhGia { get; set; }
        public string TenKyDanhGia { get; set; } = string.Empty;
        public List<TieuChi> DanhSachTieuChi { get; set; } = new List<TieuChi>();
        public List<TrongSoTieuChi> TrongSos { get; set; } = new List<TrongSoTieuChi>();
        public List<NhaCungCap> DanhSachNCC { get; set; } = new List<NhaCungCap>();

        // Điểm: DiemMap[MaNCC][MaTieuChi] = Điểm (1-9)
        public Dictionary<int, Dictionary<int, decimal>> DiemMap { get; set; } = new Dictionary<int, Dictionary<int, decimal>>();
    }

    public class KetQuaXepHangViewModel
    {
        public int MaKyDanhGia { get; set; }
        public string TenKyDanhGia { get; set; } = string.Empty;
        public List<TieuChi> DanhSachTieuChi { get; set; } = new List<TieuChi>();
        public List<TrongSoTieuChi> TrongSos { get; set; } = new List<TrongSoTieuChi>();
        public List<KetQuaXepHang> BangXepHang { get; set; } = new List<KetQuaXepHang>();
        public Dictionary<int, Dictionary<int, decimal>> DiemMap { get; set; } = new Dictionary<int, Dictionary<int, decimal>>();
        public KetQuaXepHang? NhaCungCapDeXuat { get; set; }
    }
}
