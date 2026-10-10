using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("DanhGiaNPP")]
    public class DanhGiaNPP
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaDanhGia { get; set; }

        [Required]
        public int MaDonHang { get; set; }

        [Required]
        public int MaNPPMua { get; set; }

        [Required]
        public int MaNPPBan { get; set; }

        [Range(1, 5)]
        public int SoSao { get; set; } = 5;

        [MaxLength(500)]
        public string? NoiDung { get; set; }

        public DateTime? NgayDanhGia { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("MaDonHang")]
        public virtual DonHangB2B? DonHang { get; set; }

        [ForeignKey("MaNPPMua")]
        public virtual DoanhNghiep? NPPMua { get; set; }

        [ForeignKey("MaNPPBan")]
        public virtual DoanhNghiep? NPPBan { get; set; }
    }
}
