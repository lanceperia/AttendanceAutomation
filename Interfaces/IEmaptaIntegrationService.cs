using AttendanceAutomation.Models;

namespace AttendanceAutomation.Interfaces
{
    public interface IEmaptaIntegrationService
    {
        bool HasToken();
        bool IsTokenRefreshed();
        bool HasClockedIn();
        bool HasClockedOut();
        
        AttendanceItem? GetAttendanceDetails();
    }
}
