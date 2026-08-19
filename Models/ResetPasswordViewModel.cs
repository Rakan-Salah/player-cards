using System.ComponentModel.DataAnnotations;
public class ResetPasswordViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string NewPassword { get; set; } = "";

    [Required, DataType(DataType.Password), Compare("NewPassword")]
    public string ConfirmPassword { get; set; } = "";

    public string Token { get; set; } = "";
}
