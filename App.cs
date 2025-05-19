using AttendanceAutomation.Interfaces;
using AttendanceAutomation.Models;

namespace AttendanceAutomation
{
    public class App(ILoggerService logger,
        IConnectionService connectionService,
        IEmaptaIntegrationService emaptaService,
        IEmailNotificationService emailService)
    {
        public void Run()
        {
            // Check if there's internet connection
            if (!connectionService.HasInternetConnection())
            {
                return;
            }

            if (!emaptaService.HasToken())
            {
                logger.Information("No reference token set up");
                emailService.SendEmail("Failed", "Setup issue");

                return;
            }

            if (!emaptaService.IsTokenRefreshed())
            {
                logger.Information("ISSUE WITH REFRESH TOKEN :(");

                emailService.SendEmail("Failed", "Session's expired :(");

                return;
            }

            // TO-DO: HOLIDAY CHECKING
            // INSERT HERE

            var attendanceDetails = emaptaService.GetAttendanceDetails();

            if (IsStatusEqual(attendanceDetails.Status, AttendanceItem.COMPLETED))
            {
                emailService.SendEmail("Shift is done", $"Shift is completed");

                return;
            }

            if (IsStatusEqual(attendanceDetails.Status, AttendanceItem.STARTED))
            {
                ProcessDtr("Clock Out", emaptaService.HasClockedOut);
                return;
            }

            if (IsStatusEqual(attendanceDetails.Status, AttendanceItem.NOT_STARTED))
            {
                ProcessDtr("Clock In", emaptaService.HasClockedIn);
                return;
            }
        }

        // Private Methods
        private bool IsStatusEqual(string expected, string actual)
        {
            return expected.Equals(actual, StringComparison.OrdinalIgnoreCase);
        }
        private void ProcessDtr(string action, Func<bool> hasClockedFunc)
        {
            if (hasClockedFunc())
            {
                logger.Information($"{action} Successfully!");

                emailService.SendEmail(action, $"{action} at {DateTime.Now:t}");
                return;
            }

            emailService.SendEmail(action, $"{action} failed");
        }
    }
}
