using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("KetQuaAHP_B2B")]
    public class KetQuaAHP_B2B
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaKetQua { get; set; }

        [Required]
        public int MaPhien { get; set; }

        [Required]
        public int MaDoanhNghiep { get; set; }

        [Column(TypeName = "decimal(4,2)")]
        public decimal DiemGia { get; set; } = 0;

        [Column(TypeName = "decimal(4,2)")]
        public decimal DiemGiaoHang { get; set; } = 0;

        [Column(TypeName = "decimal(4,2)")]
        public decimal DiemUyTin { get; set; } = 0;

        [Column(TypeName = "decimal(4,2)")]
        public decimal DiemTonKho { get; set; } = 0;

        [Column(TypeName = "decimal(6,3)")]
        public decimal DiemTongHop { get; set; } = 0;

        public int XepHang { get; set; } = 1;

        public bool DeXuat { get; set; } = false;

        // Navigation properties
        [ForeignKey("MaPhien")]
        public virtual PhienAHP_B2B? PhienAHP { get; set; }

        [ForeignKey("MaDoanhNghiep")]
        public virtual DoanhNghiep? DoanhNghiep { get; set; }
    }
}
