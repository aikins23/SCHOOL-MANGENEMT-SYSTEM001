using System;
using System.Linq;
using System.Threading.Tasks;
using KingdomPrep.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace KingdomPrep.Web.Data;

public class UserProfileDto
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? AvatarBase64 { get; set; }
}

public interface IUserProfileService
{
    Task<UserProfileDto?> GetProfileAsync(string username);
    Task<bool> UpdateProfileAsync(string username, string fullName, string phone, string avatarBase64);
}

public class UserProfileService(IDbContextFactory<AppDbContext> dbFactory) : IUserProfileService
{
    public async Task<UserProfileDto?> GetProfileAsync(string username)
    {
        using var db = await dbFactory.CreateDbContextAsync();

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return null;

        if (user.EmploymentID.HasValue)
        {
            var emp = await db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.EmployeeID == user.EmploymentID.Value);
            if (emp != null)
            {
                return new UserProfileDto
                {
                    FullName = string.IsNullOrWhiteSpace(emp.FullName) ? username : emp.FullName,
                    Phone = emp.Contact,
                    AvatarBase64 = emp.ProfilePhoto != null && emp.ProfilePhoto.Length > 0
                        ? $"data:image/jpeg;base64,{Convert.ToBase64String(emp.ProfilePhoto)}"
                        : null
                };
            }
        }
        else if (user.UserType == "Student" && int.TryParse(username, out int studentId))
        {
            var student = await db.Students.AsNoTracking().FirstOrDefaultAsync(s => s.StudentID == studentId);
            if (student != null)
            {
                return new UserProfileDto
                {
                    FullName = student.FullName,
                    Phone = student.EmergencyContact,
                    AvatarBase64 = student.ProfilePhoto != null && student.ProfilePhoto.Length > 0
                        ? $"data:image/jpeg;base64,{Convert.ToBase64String(student.ProfilePhoto)}"
                        : null
                };
            }
        }

        return new UserProfileDto { FullName = username };
    }

    public async Task<bool> UpdateProfileAsync(string username, string fullName, string phone, string avatarBase64)
    {
        using var db = await dbFactory.CreateDbContextAsync();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null) return false;

        byte[]? avatarBytes = null;
        if (!string.IsNullOrEmpty(avatarBase64) && avatarBase64.Contains(","))
        {
            try
            {
                var base64Data = avatarBase64.Substring(avatarBase64.IndexOf(",") + 1);
                avatarBytes = Convert.FromBase64String(base64Data);
            }
            catch { }
        }

        if (user.EmploymentID.HasValue)
        {
            var emp = await db.Employees.FirstOrDefaultAsync(e => e.EmployeeID == user.EmploymentID.Value);
            if (emp != null)
            {
                emp.FullName = fullName;
                emp.Contact = phone;
                if (avatarBytes != null) emp.ProfilePhoto = avatarBytes;

                await db.SaveChangesAsync();
                return true;
            }
        }
        else if (user.UserType == "Student" && int.TryParse(username, out int studentId))
        {
            var student = await db.Students.FirstOrDefaultAsync(s => s.StudentID == studentId);
            if (student != null)
            {
                student.EmergencyContact = phone;
                if (avatarBytes != null) student.ProfilePhoto = avatarBytes;

                await db.SaveChangesAsync();
                return true;
            }
        }

        return false;
    }
}
