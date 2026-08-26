using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("ChiTietPhieuNhap")]
    public class ChiTietPhieuNhap
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaChiTiet { get; set; }

        public int? MaPhieuNhap { get; set; }

        public int? MaSanPham { get; set; }

        public int? SoLuong { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DonGia { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column(TypeName = "decimal(29,2)")]
        public decimal? ThanhTien { get; set; }

        // Navigation properties
        [ForeignKey("MaPhieuNhap")]
        public virtual PhieuNhap? PhieuNhap { get; set; }

        [ForeignKey("MaSanPham")]
        public virtual SanPham? SanPham { get; set; }
    }
}
