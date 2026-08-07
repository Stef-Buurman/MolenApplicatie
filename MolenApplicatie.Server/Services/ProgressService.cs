using Hangfire.Console;
using Hangfire.Server;

namespace MolenApplicatie.Server.Services
{
    public sealed class ProgressService
    {
        private readonly ProgressTracker _progressState;
        private readonly PerformContext? _performContext;
        private readonly Action<double>? _setProgressBarValue;

        private int _totalAmount;
        private int _currentAmount;

        public ProgressService(ProgressTracker progressState, PerformContext? performContext)
        {
            _progressState = progressState;
            _performContext = performContext;

            if (_performContext != null)
            {
                var progressBar = _performContext.WriteProgressBar();
                _setProgressBarValue = value => progressBar.SetValue(value);
            }
        }

        public int ProgressPercentage => _progressState.ProgressPercentage;

        public ProgressService SetProgressMessage(string message, bool logToConsole = true)
        {
            _progressState.SetProgressMessage(message);

            if (logToConsole) _performContext?.WriteLine(message);

            return this;
        }

        public ProgressService SetProgressPercentage(int percentage)
        {
            var clampedPercentage = Math.Clamp(percentage, 0, 100);

            _progressState.SetProgressPercentage(clampedPercentage);
            _setProgressBarValue?.Invoke(clampedPercentage);

            return this;
        }

        public ProgressService SetTotalAmount(int totalAmount)
        {
            _totalAmount = Math.Max(totalAmount, 0);
            _currentAmount = 0;

            return SetProgressAmount(0);
        }

        public ProgressService SetCurrentAmount(int amount)
        {
            _currentAmount += amount;
            return SetProgressAmount(_currentAmount);
        }

        public ProgressService SetProgressAmount(int amount)
        {
            _currentAmount = Math.Max(amount, 0);

            if (_totalAmount > 0) _currentAmount = Math.Min(_currentAmount, _totalAmount);
            var percentage = _totalAmount == 0 ? 0 : (int)(_currentAmount / (double)_totalAmount * 100);
            _progressState.SetProgressAmount($"{_currentAmount:N0}/{_totalAmount:N0}");

            return SetProgressPercentage(percentage);
        }

        public ProgressService SetStepNumber(int step)
        {
            _progressState.SetStepNumber(step);
            return this;
        }

        public ProgressService SetIsActive(bool active)
        {
            _progressState.SetIsActive(active);
            return this;
        }

        public ProgressService Reset(bool stillActive = false, bool keepSteps = false)
        {
            _progressState.Reset(stillActive, keepSteps);
            _totalAmount = 0;
            _currentAmount = 0;
            _setProgressBarValue?.Invoke(0);

            return this;
        }

        public ProgressService SetIsDoneSuccessfully(string? message = null)
        {
            SetProgressPercentage(100);
            _progressState.SetIsDone();

            return SetProgressMessage(message ?? "Job completed successfully!");
        }

        public ProgressService SetIsDoneWithErrors(string? errorMessage = null)
        {
            _progressState.SetIsError();

            return SetProgressMessage(errorMessage ?? "Job completed with errors!");
        }

        public ProgressService SetIsDone()
        {
            _progressState.SetIsDone();
            return this;
        }

        public ProgressService SetIsCancelled(string? message = null)
        {
            _progressState.SetIsCancelled();

            return SetProgressMessage(message ?? "Job was cancelled.");
        }

        public ProgressService SetIsError()
        {
            _progressState.SetIsError();
            return this;
        }

        public ProgressService Commit()
        {
            return this;
        }
    }
}
