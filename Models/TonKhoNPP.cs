using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("TonKhoNPP")]
    public class TonKhoNPP
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaTonKhoNPP { get; set; }

        public int MaDoanhNghiep { get; set; }

        public int MaSanPham { get; set; }

        public int SoLuongTon { get; set; } = 0;

        [Column(TypeName = "decimal(12,2)")]
        public decimal GiaSi { get; set; } = 0;

        /// <summary>
        /// Số ngày cam kết giao hàng
        /// </summary>
        public int ThoiGianGiaoHang { get; set; } = 1;

        /// <summary>
        /// Có đang chào bán trên sàn B2B không
        /// </summary>
        public bool DangChaoBan { get; set; } = false;

        public DateTime? NgayCapNhat { get; set; }

        // Navigation properties
        [ForeignKey("MaDoanhNghiep")]
        public virtual DoanhNghiep? DoanhNghiep { get; set; }

        [ForeignKey("MaSanPham")]
        public virtual SanPham? SanPham { get; set; }
    }
}
