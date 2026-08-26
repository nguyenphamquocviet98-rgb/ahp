using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace code.Models
{
    [Table("DanhMuc")]
    public class DanhMuc
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaDanhMuc { get; set; }

        [MaxLength(50)]
        public string? TenDanhMuc { get; set; }

        [MaxLength(200)]
        public string? MoTa { get; set; }

        // Navigation properties
        public virtual ICollection<SanPham> SanPhams { get; set; } = new List<SanPham>();
    }
}
