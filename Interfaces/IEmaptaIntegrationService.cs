namespace AttendanceAutomation.Interfaces
{
    public interface IEmaptaIntegrationService
    {
        bool HasToken();
        bool IsTokenRefreshed();
        bool HasClockedIn();
        bool HasClockedOut();
        bool IsNewShift();
        bool IsShiftCompleted();
        bool IsShiftStarted();

    }
}
