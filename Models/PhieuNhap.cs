using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("PhieuNhap")]
    public class PhieuNhap
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaPhieuNhap { get; set; }

        [MaxLength(20)]
        public string? MaPhieuNhapCode { get; set; }

        public int? MaNhaCungCap { get; set; }

        public int? MaNguoiDung { get; set; }

        [Column(TypeName = "date")]
        public DateTime? NgayNhap { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? TongTien { get; set; }

        [MaxLength(20)]
        public string? TrangThai { get; set; }

        public DateTime? NgayTao { get; set; }

        // Navigation properties
        [ForeignKey("MaNhaCungCap")]
        public virtual NhaCungCap? NhaCungCap { get; set; }

        [ForeignKey("MaNguoiDung")]
        public virtual NguoiDung? NguoiDung { get; set; }

        public virtual ICollection<ChiTietPhieuNhap> ChiTietPhieuNhaps { get; set; } = new List<ChiTietPhieuNhap>();
        public virtual ICollection<LichSuNhapHang> LichSuNhapHangs { get; set; } = new List<LichSuNhapHang>();
    }
}
