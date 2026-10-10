using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("DonHangB2B")]
    public class DonHangB2B
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaDonHang { get; set; }

        [Required]
        [MaxLength(20)]
        public string MaDonHangCode { get; set; } = string.Empty;

        [Required]
        public int MaNPPMua { get; set; }

        [Required]
        public int MaNPPBan { get; set; }

        [Required]
        public int MaNguoiDat { get; set; }

        public DateTime? NgayDatHang { get; set; } = DateTime.Now;

        [Column(TypeName = "decimal(15,2)")]
        public decimal TongTien { get; set; } = 0;

        [MaxLength(20)]
        public string TrangThai { get; set; } = "cho_duyet"; // cho_duyet, da_duyet, dang_giao, hoan_thanh, huy

        [MaxLength(500)]
        public string? GhiChu { get; set; }

        public string? LichSuXuLyJson { get; set; }

        public DateTime? NgayCapNhat { get; set; } = DateTime.Now;

        // Navigation properties
        [ForeignKey("MaNPPMua")]
        public virtual DoanhNghiep? NPPMua { get; set; }

        [ForeignKey("MaNPPBan")]
        public virtual DoanhNghiep? NPPBan { get; set; }

        [ForeignKey("MaNguoiDat")]
        public virtual NguoiDung? NguoiDat { get; set; }

        public virtual ICollection<ChiTietDonHangB2B> ChiTietDonHangs { get; set; } = new List<ChiTietDonHangB2B>();
    }
}
