using System.Threading.Tasks;

namespace kingdom_Preparatory_School_Management_System.Services
{
    /// <summary>One concrete SMS delivery channel (Arkesel, log, etc.).</summary>
    public interface ISmsProvider
    {
        string Name { get; }
        Task<(bool Success, string Message)> SendAsync(string senderId, string recipient233, string message);
    }
}
