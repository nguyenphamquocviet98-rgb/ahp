using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("NguoiDung")]
    public class NguoiDung
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaNguoiDung { get; set; }

        [MaxLength(30)]
        public string? TenDangNhap { get; set; }

        [MaxLength(100)]
        public string? MatKhau { get; set; }

        [MaxLength(20)]
        public string? VaiTro { get; set; }

        public bool? TrangThai { get; set; }

        public DateTime? NgayTao { get; set; }

        /// <summary>
        /// Liên kết tới Doanh nghiệp/NPP. NULL nếu là Admin Sàn.
        /// </summary>
        public int? MaDoanhNghiep { get; set; }

        // Navigation properties
        [ForeignKey("MaDoanhNghiep")]
        public virtual DoanhNghiep? DoanhNghiep { get; set; }

        public virtual ICollection<PhieuNhap> PhieuNhaps { get; set; } = new List<PhieuNhap>();
        public virtual ICollection<KyDanhGia> KyDanhGias { get; set; } = new List<KyDanhGia>();
    }
}
