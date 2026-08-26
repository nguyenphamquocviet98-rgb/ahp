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

        // Navigation properties
        public virtual ICollection<PhieuNhap> PhieuNhaps { get; set; } = new List<PhieuNhap>();
        public virtual ICollection<KyDanhGia> KyDanhGias { get; set; } = new List<KyDanhGia>();
    }
}
