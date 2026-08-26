using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("SanPham")]
    public class SanPham
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaSanPham { get; set; }

        [MaxLength(20)]
        public string? MaSanPhamCode { get; set; }

        [MaxLength(100)]
        public string? TenSanPham { get; set; }

        public int? MaDanhMuc { get; set; }

        public int? MaDonViTinh { get; set; }

        [MaxLength(200)]
        public string? AnhSanPham { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? GiaMacDinh { get; set; }

        public bool? TrangThai { get; set; }

        // Navigation properties
        [ForeignKey("MaDanhMuc")]
        public virtual DanhMuc? DanhMuc { get; set; }

        [ForeignKey("MaDonViTinh")]
        public virtual DonViTinh? DonViTinh { get; set; }

        public virtual TonKho? TonKho { get; set; }
        public virtual ICollection<ChiTietPhieuNhap> ChiTietPhieuNhaps { get; set; } = new List<ChiTietPhieuNhap>();
        public virtual ICollection<LichSuNhapHang> LichSuNhapHangs { get; set; } = new List<LichSuNhapHang>();
    }
}
