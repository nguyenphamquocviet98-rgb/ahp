using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("DoanhNghiep")]
    public class DoanhNghiep
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaDoanhNghiep { get; set; }

        [Required]
        [MaxLength(150)]
        public string TenDoanhNghiep { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? MaSoThue { get; set; }

        [MaxLength(200)]
        public string? DiaChi { get; set; }

        [MaxLength(15)]
        public string? SoDienThoai { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(300)]
        public string? Logo { get; set; }

        [MaxLength(500)]
        public string? MoTa { get; set; }

        // Cơ chế Mall
        public bool LaGianHangMall { get; set; } = false;

        public DateTime? NgayThamGiaMall { get; set; }

        public DateTime? HanMall { get; set; }

        [MaxLength(300)]
        public string? GiayPhepKinhDoanh { get; set; }

        [MaxLength(20)]
        public string TrangThaiMall { get; set; } = "thuong";

        // Uy tín (thang 1-10, trung bình từ đánh giá)
        [Column(TypeName = "decimal(3,1)")]
        public decimal DiemUyTin { get; set; } = 5.0m;

        public DateTime? NgayTao { get; set; }

        public bool TrangThai { get; set; } = true;

        // Navigation properties
        public virtual ICollection<NguoiDung> NguoiDungs { get; set; } = new List<NguoiDung>();
        public virtual ICollection<TonKhoNPP> TonKhoNPPs { get; set; } = new List<TonKhoNPP>();
    }
}
