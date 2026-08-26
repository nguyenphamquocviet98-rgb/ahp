using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("LichSuNhapHang")]
    public class LichSuNhapHang
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaLichSu { get; set; }

        public int? MaSanPham { get; set; }

        public int? MaPhieuNhap { get; set; }

        public int? SoLuongThayDoi { get; set; }

        [MaxLength(20)]
        public string? LoaiThayDoi { get; set; }

        public DateTime? NgayTao { get; set; }

        // Navigation properties
        [ForeignKey("MaSanPham")]
        public virtual SanPham? SanPham { get; set; }

        [ForeignKey("MaPhieuNhap")]
        public virtual PhieuNhap? PhieuNhap { get; set; }
    }
}
