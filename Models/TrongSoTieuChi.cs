using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("TrongSoTieuChi")]
    public class TrongSoTieuChi
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaTrongSo { get; set; }

        public int? MaKyDanhGia { get; set; }

        public int? MaTieuChi { get; set; }

        [Column(TypeName = "decimal(5,3)")]
        public decimal? TrongSo { get; set; }

        [Column(TypeName = "decimal(6,4)")]
        public decimal? LambdaMax { get; set; }

        [Column(TypeName = "decimal(6,4)")]
        public decimal? CI { get; set; }

        [Column(TypeName = "decimal(6,4)")]
        public decimal? CR { get; set; }

        // Navigation properties
        [ForeignKey("MaKyDanhGia")]
        public virtual KyDanhGia? KyDanhGia { get; set; }

        [ForeignKey("MaTieuChi")]
        public virtual TieuChi? TieuChi { get; set; }
    }
}
