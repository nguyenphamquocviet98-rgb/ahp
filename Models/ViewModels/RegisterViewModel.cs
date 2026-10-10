using System.ComponentModel.DataAnnotations;

namespace code.Models.ViewModels
{
    public class RegisterViewModel
    {
        // Thông tin tài khoản
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [Display(Name = "Tên đăng nhập")]
        [MaxLength(30)]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        public string MatKhau { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu")]
        [Compare("MatKhau", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        public string XacNhanMatKhau { get; set; } = string.Empty;

        // Thông tin doanh nghiệp
        [Required(ErrorMessage = "Vui lòng nhập tên doanh nghiệp")]
        [Display(Name = "Tên doanh nghiệp / Nhà phân phối")]
        [MaxLength(150)]
        public string TenDoanhNghiep { get; set; } = string.Empty;

        [Display(Name = "Mã số thuế")]
        [MaxLength(20)]
        public string? MaSoThue { get; set; }

        [Display(Name = "Địa chỉ")]
        [MaxLength(200)]
        public string? DiaChi { get; set; }

        [Display(Name = "Số điện thoại doanh nghiệp")]
        [MaxLength(15)]
        public string? SoDienThoaiDN { get; set; }

        [Display(Name = "Email doanh nghiệp")]
        [MaxLength(100)]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? EmailDN { get; set; }

        [Display(Name = "Mô tả hoạt động kinh doanh")]
        [MaxLength(500)]
        public string? MoTa { get; set; }
    }
}
