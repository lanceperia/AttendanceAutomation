using AttendanceAutomation.Interfaces;

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

            if (!emaptaService.HasToken() || !emaptaService.IsTokenRefreshed())
            {
                logger.Information("ISSUE WITH REFRESH TOKEN :(");

                emailService.SendEmail("Failed", "Session's expired :(");

                return;
            }

            if (emaptaService.IsShiftCompleted())
            {
                emailService.SendEmail("Shift is done", $"Shift is completed");

                return;
            }

            if (emaptaService.IsShiftStarting())
            {
                ProcessClockIn();

                return;
            }

            if (emaptaService.IsShiftEnding())
            {
                ProcessClockOut();
                return;
            }

        }

        // Private Methods

        private void ProcessClockIn()
        {
            if (emaptaService.HasClockedIn())
            {
                logger.Information("Clocked In Successfully!");

                emailService.SendEmail("Clock In", $"Clocked in at {DateTime.Now:t}");
                return;
            }

            emailService.SendEmail("Failed", $"Clock In failed");
        }

        private void ProcessClockOut()
        {
            if (emaptaService.HasClockedOut())
            {
                logger.Information("Clocked Out Successfully!");

                emailService.SendEmail("Clock out", $"Clocked out at {DateTime.Now:t}");
                return;
            }

            emailService.SendEmail("Failed", $"Clock out failed");
        }
    }
}
