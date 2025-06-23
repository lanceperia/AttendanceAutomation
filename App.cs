using AttendanceAutomation.Interfaces;
using AttendanceAutomation.Models;
using System;

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

            var attendanceDetails = emaptaService.GetAttendanceDetails();

            // Check Rest day
            var isRestDay = attendanceDetails.IsRestday ?? false;
            if (isRestDay 
                || IsStatusEqual(attendanceDetails.Status, AttendanceItem.RESTDAY)
                || IsStatusEqual(attendanceDetails.Status, AttendanceItem.ON_LEAVE))
            {
                emailService.SendEmail("Restday", $"Don't bother working");
                return;
            }

            // Check if DTR is completed
            if (IsStatusEqual(attendanceDetails.Status, AttendanceItem.COMPLETED))
            {
                emailService.SendEmail("Shift is done", $"Shift is completed");
                return;
            }

            // Check if DTR is ready to clock out
            var hasTimeIn = !string.IsNullOrWhiteSpace(attendanceDetails.DateTimeIn);
            if (hasTimeIn && !isRestDay)
            {
                ProcessDtr("Out", emaptaService.HasClockedOut);
                return;
            }

            // Check if DTR is ready to clock in
            if (!hasTimeIn && !isRestDay)
            {
                ProcessDtr("In", emaptaService.HasClockedIn);
                return;
            }

            logger.Information("Automation didn't trigger");
            emailService.SendEmail("Automation FAILED", "Clock In/Out manually D:");
        }

        // Private Methods
        private bool IsStatusEqual(string actual, string expected)
        {
            return expected.Equals(actual, StringComparison.OrdinalIgnoreCase);
        }
        private void ProcessDtr(string action, Func<bool> hasClockedFunc)
        {
            if (hasClockedFunc())
            {
                logger.Information($"Clocked {action} Successfully!");

                emailService.SendEmail("DTR", $"Clocked {action} at {DateTime.Now:t}");
                return;
            }

            emailService.SendEmail("DTR", $"Clock {action} failed");
        }
    }
}
