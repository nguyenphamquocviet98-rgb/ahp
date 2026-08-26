using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("KyDanhGia")]
    public class KyDanhGia
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaKyDanhGia { get; set; }

        [MaxLength(50)]
        public string? TenKyDanhGia { get; set; }

        public int? MaNguoiDung { get; set; }

        public DateTime? NgayTao { get; set; }

        [MaxLength(20)]
        public string? TrangThai { get; set; }

        // Navigation properties
        [ForeignKey("MaNguoiDung")]
        public virtual NguoiDung? NguoiDung { get; set; }

        public virtual ICollection<MaTranSoSanh> MaTranSoSanhs { get; set; } = new List<MaTranSoSanh>();
        public virtual ICollection<TrongSoTieuChi> TrongSoTieuChis { get; set; } = new List<TrongSoTieuChi>();
        public virtual ICollection<DiemDanhGiaNCC> DiemDanhGiaNCCs { get; set; } = new List<DiemDanhGiaNCC>();
        public virtual ICollection<KetQuaXepHang> KetQuaXepHangs { get; set; } = new List<KetQuaXepHang>();
    }
}
