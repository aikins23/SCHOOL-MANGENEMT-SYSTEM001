using System.Text;
using KingdomPrep.Web.Data.Entities;

namespace KingdomPrep.Web.Data;

public static class SchoolBranding
{
    public const string ProductName = "Nyansapo School ERP";

    public static string DisplayName(SchoolInfoEntity? profile)
        => string.IsNullOrWhiteSpace(profile?.Name) ? ProductName : profile.Name.Trim();

    public static string Initials(SchoolInfoEntity? profile)
        => Initials(DisplayName(profile));

    public static string FormatStudentId(int studentId, SchoolInfoEntity? profile)
    {
        var prefix = Initials(profile);
        return string.IsNullOrWhiteSpace(prefix) ? studentId.ToString() : $"{prefix}{studentId}";
    }

    public static string FormatStudentId(int? studentId, SchoolInfoEntity? profile)
        => studentId.HasValue ? FormatStudentId(studentId.Value, profile) : "";

    public static string Initials(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "";
        }

        var initials = new StringBuilder();
        foreach (var word in value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var first = word.FirstOrDefault(char.IsLetterOrDigit);
            if (first != default)
            {
                initials.Append(char.ToUpperInvariant(first));
            }

            if (initials.Length >= 6)
            {
                break;
            }
        }

        return initials.Length == 0 ? "" : initials.ToString();
    }
}
