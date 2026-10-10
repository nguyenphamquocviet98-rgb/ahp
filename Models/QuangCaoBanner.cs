using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("QuangCaoBanner")]
    public class QuangCaoBanner
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaBanner { get; set; }

        [Required]
        public int MaDoanhNghiep { get; set; }

        [Required]
        [MaxLength(150)]
        public string TieuDe { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? HinhAnh { get; set; }

        [MaxLength(200)]
        public string? LienKet { get; set; }

        [MaxLength(30)]
        public string ViTri { get; set; } = "trang_nhat";

        public DateTime NgayBatDau { get; set; } = DateTime.Now;

        public DateTime NgayKetThuc { get; set; } = DateTime.Now.AddMonths(1);

        [Column(TypeName = "decimal(12,2)")]
        public decimal ChiPhi { get; set; } = 0;

        [MaxLength(20)]
        public string TrangThai { get; set; } = "cho_duyet"; // cho_duyet, hoat_dong, het_han, tu_choi

        public DateTime? NgayTao { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("MaDoanhNghiep")]
        public virtual DoanhNghiep? DoanhNghiep { get; set; }
    }
}
