using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("PhienAHP_B2B")]
    public class PhienAHP_B2B
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaPhien { get; set; }

        [Required]
        public int MaNPPMua { get; set; }

        [Required]
        public int MaSanPham { get; set; }

        public int SoLuongCanMua { get; set; } = 1;

        public DateTime? NgayTao { get; set; } = DateTime.Now;

        [MaxLength(20)]
        public string TrangThai { get; set; } = "hoan_thanh";

        public int? MaNPPDeXuat { get; set; }

        [MaxLength(500)]
        public string? GhiChu { get; set; }

        public string? SnapshotJson { get; set; }

        // Navigation properties
        [ForeignKey("MaNPPMua")]
        public virtual DoanhNghiep? NPPMua { get; set; }

        [ForeignKey("MaSanPham")]
        public virtual SanPham? SanPham { get; set; }

        [ForeignKey("MaNPPDeXuat")]
        public virtual DoanhNghiep? NPPDeXuat { get; set; }

        public virtual ICollection<KetQuaAHP_B2B> KetQuaAHPs { get; set; } = new List<KetQuaAHP_B2B>();
    }
}
