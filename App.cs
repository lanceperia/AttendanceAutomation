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

        private const string EMAIL_SUBJECT = "DTR";

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
            if (attendanceDetails is null)
            {
                emailService.SendEmail(EMAIL_SUBJECT, "API Failed");
            }

            // Check Rest day
            var isRestDay = attendanceDetails.IsRestday ?? false;
            if (isRestDay
                || IsStatusEqual(attendanceDetails.Status, AttendanceItem.RESTDAY)
                || IsStatusEqual(attendanceDetails.Status, AttendanceItem.ON_LEAVE))
            {
                emailService.SendEmail(EMAIL_SUBJECT, $"Don't bother working");
                return;
            }

            // Check if DTR is completed
            if (IsStatusEqual(attendanceDetails.Status, AttendanceItem.COMPLETED))
            {
                emailService.SendEmail(EMAIL_SUBJECT, $"Shift is completed");
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
            emailService.SendEmail(EMAIL_SUBJECT, "Clock In/Out manually D:");
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

                emailService.SendEmail(EMAIL_SUBJECT, $"Clocked {action} at {DateTime.Now:t}");
                return;
            }

            emailService.SendEmail(EMAIL_SUBJECT, $"Clock {action} failed");
        }
    }
}
