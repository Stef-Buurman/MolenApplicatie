namespace MolenApplicatie.Server.Services
{
    public sealed class ProgressTracker
    {
        public string ProgressMessage { get; private set; } = string.Empty;
        public string ProgressAmount { get; private set; } = string.Empty;
        public int ProgressPercentage { get; private set; }
        public int StepNumber { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsDone { get; private set; }
        public bool IsCancelled { get; private set; }
        public bool IsError { get; private set; }

        public void SetProgressMessage(string message)
        {
            ProgressMessage = message;
        }

        public void SetProgressAmount(string amount)
        {
            ProgressAmount = amount;
        }

        public void SetProgressPercentage(int percentage)
        {
            ProgressPercentage = Math.Clamp(percentage, 0, 100);
        }

        public void SetStepNumber(int step)
        {
            StepNumber = step;
        }

        public void SetIsActive(bool active)
        {
            IsActive = active;

            if (active)
            {
                IsDone = false;
                IsCancelled = false;
                IsError = false;
            }
        }

        public void SetIsDone()
        {
            IsDone = true;
            IsActive = false;
        }

        public void SetIsCancelled()
        {
            IsCancelled = true;
            IsError = false;
            SetIsDone();
        }

        public void SetIsError()
        {
            IsError = true;
            IsCancelled = false;
            SetIsDone();
        }

        public void Reset(bool stillActive = false, bool keepSteps = false)
        {
            ProgressMessage = string.Empty;
            ProgressAmount = string.Empty;
            ProgressPercentage = 0;
            StepNumber = keepSteps ? StepNumber : 0;
            IsActive = stillActive;
            IsDone = false;
            IsCancelled = false;
            IsError = false;
        }
    }

    public static class ProgressState
    {
        public static readonly ProgressTracker MillDatabaseImportProgress = new();
    }
}
