using System.Text;

namespace Gcmac.Registration.Models;

public static class MembershipIds
{
    public static string Build(string? memberNo, string? name)
    {
        var member = (memberNo ?? "").Trim();
        if (member.Length == 0) return "";

        var slug = new StringBuilder();
        var dash = false;
        foreach (var character in (name ?? "").Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                slug.Append(character);
                dash = false;
            }
            else if (!dash && slug.Length > 0)
            {
                slug.Append('-');
                dash = true;
            }
        }

        var suffix = slug.ToString().Trim('-').ToUpperInvariant();
        return suffix.Length == 0 ? member : $"{member}-{suffix}";
    }
}
