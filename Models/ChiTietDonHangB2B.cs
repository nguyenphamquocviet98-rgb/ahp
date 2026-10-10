using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("ChiTietDonHangB2B")]
    public class ChiTietDonHangB2B
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaChiTiet { get; set; }

        [Required]
        public int MaDonHang { get; set; }

        [Required]
        public int MaSanPham { get; set; }

        public int SoLuong { get; set; } = 1;

        [Column(TypeName = "decimal(12,2)")]
        public decimal DonGia { get; set; } = 0;

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [Column(TypeName = "decimal(25,2)")]
        public decimal? ThanhTien { get; set; }

        // Navigation properties
        [ForeignKey("MaDonHang")]
        public virtual DonHangB2B? DonHang { get; set; }

        [ForeignKey("MaSanPham")]
        public virtual SanPham? SanPham { get; set; }
    }
}
