using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("DoanhThuSan")]
    public class DoanhThuSan
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaGiaoDich { get; set; }

        [Required]
        public int MaDoanhNghiep { get; set; }

        [Required]
        [MaxLength(30)]
        public string LoaiThu { get; set; } = "phi_quang_cao"; // phi_mall, phi_quang_cao

        [Column(TypeName = "decimal(15,2)")]
        public decimal SoTien { get; set; } = 0;

        public DateTime? NgayGiaoDich { get; set; } = DateTime.Now;

        [MaxLength(300)]
        public string? MoTa { get; set; }

        [MaxLength(20)]
        public string TrangThai { get; set; } = "da_thu"; // da_thu, chua_thu

        // Navigation properties
        [ForeignKey("MaDoanhNghiep")]
        public virtual DoanhNghiep? DoanhNghiep { get; set; }
    }
}
