namespace AttendanceAutomation.Interfaces
{
    public interface IEmaptaIntegrationService
    {
        bool HasToken();
        bool IsTokenRefreshed();
        bool HasClockedIn();
        bool HasClockedOut();
        bool IsShiftStarting();
        bool IsShiftCompleted();
        bool IsShiftEnding();

    }
}
