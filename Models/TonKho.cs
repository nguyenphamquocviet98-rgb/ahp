using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("TonKho")]
    public class TonKho
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaTonKho { get; set; }

        public int? MaSanPham { get; set; }

        public int? SoLuongTon { get; set; }

        public DateTime? NgayCapNhat { get; set; }

        // Navigation properties
        [ForeignKey("MaSanPham")]
        public virtual SanPham? SanPham { get; set; }
    }
}
