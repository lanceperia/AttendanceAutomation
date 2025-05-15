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


            if (emaptaService.IsShiftCompleted())
            {
                emailService.SendEmail("Shift is done", $"Shift is completed");

                return;
            }

            if (emaptaService.IsShiftStarted())
            {
                ProcessDtr("Clock Out", emaptaService.HasClockedOut);
                return;
            }

            if (emaptaService.IsNewShift())
            {
                ProcessDtr("Clock In", emaptaService.HasClockedIn);
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
