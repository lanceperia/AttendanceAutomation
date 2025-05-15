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
                ProcessDtr("Clock In", emaptaService.HasClockedIn);
                return;
            }

            if (emaptaService.IsShiftEnding())
            {
                ProcessDtr("Clock Out", emaptaService.HasClockedIn);
                return;
            }

        }

        // Private Methods
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
