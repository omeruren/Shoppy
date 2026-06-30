namespace Shoppy.Business.Options;

public sealed class EmailSettings
{

    public string Host { get; set; } = default!;
    public int Port { get; set; }

    /// <summary> 
    /// SMTP authentication username (e.g Mailtrap username)
    /// </summary>
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;

    /// <summary>
    /// The "From" address shown in the email. Must be a valid email address.
    /// </summary>
    public string SenderEmail { get; set; } = "noreply@shoppy.com";
}
