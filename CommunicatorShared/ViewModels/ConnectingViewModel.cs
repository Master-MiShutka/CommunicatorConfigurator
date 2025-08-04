using System.Diagnostics;
using Microsoft.Extensions.Logging;
using PropertyChanged.SourceGenerator;

namespace TMP.Work.CommunicatorPSDTU.Common.ViewModels
{
    public sealed partial class ConnectingViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject, IDisposable
    {
        private readonly ILogger logger;

        /// <summary>
        /// Заголовок кнопки отмена
        /// </summary>
        [Notify(set: Setter.Private)] private string cancelCommandHeader = Resources.Strings.CANCEL_COMMAND_HEADER;

        /// <summary>
        /// Описание текущего действия
        /// </summary>
        [Notify(set: Setter.Internal)] private string? detailMessage;

        /// <summary>
        /// Значение времени с момента старта подключения
        /// </summary>
        [Notify(set: Setter.Private)] private string? elapsedTime;

        /// <summary>
        /// Флаг видимости прогресса
        /// </summary>
        [Notify(set: Setter.Public)] private bool isProgressVisible = true;

        /// <summary>
        /// Флаг наличия подключения
        /// </summary>
        [Notify(set: Setter.Private)] private bool isConnected;

        [AlsoNotify(nameof(Remaining))]
        [Notify(set: Setter.Public)] private ushort timeout;

        private System.Threading.Timer? timer;
        private readonly Stopwatch stopwatch;

        private readonly CancellationTokenSource cancellationTokenSource;

        private bool isCancelled;

        private long stopwatchStartMilliseconds;

        /// <summary>
        /// Создание модели представления для диалога подключения к устройству
        /// </summary>
        /// <param name="logger">Ссылка на журнал</param>
        /// <param name="cancellationTokenSource">Маркер отмены операции</param>
        /// <param name="timeout">Таймаут в секундах</param>
        public ConnectingViewModel(ILogger logger, CancellationTokenSource cancellationTokenSource, ushort timeout)
        {
            this.logger = logger;
            this.logger.LogTrace("ConnectingViewModel ctor");

            this.cancellationTokenSource = cancellationTokenSource;

            this.stopwatch = new Stopwatch();

            this.timeout = timeout;
        }

        ~ConnectingViewModel() => this.Dispose();

        #region Public properties

        public double Remaining => this.Timeout - ((this.stopwatch.ElapsedMilliseconds - this.stopwatchStartMilliseconds) / 1_000d);

        #endregion

        #region Private methods

        [RelayCommand(CanExecute = nameof(CanCancel))]
        private void Cancel()
        {
            this.isCancelled = true;

            this.CancelCommandHeader = Resources.UI_strings.IS_CANCELLING_COMMAND_HEADER;

            this.cancellationTokenSource.Cancel();

            this.CancelCommand.NotifyCanExecuteChanged();

            this.DetailMessage += System.Environment.NewLine;

            this.DetailMessage += Resources.Strings.OPERATION_CANCELLING;
        }

        private bool CanCancel() => this.isCancelled == false;

        private void ConnectionTimerCallback(object? state)
        {
            this.ElapsedTime = string.Format(System.Globalization.CultureInfo.CurrentCulture, Resources.Strings.PASSED_N_SECONDS, this.stopwatch.Elapsed.TotalSeconds);

            this.OnPropertyChanged(nameof(this.Remaining));
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Запуск таймеров
        /// </summary>
        public void StartTimers()
        {
            this.stopwatch.Start();
            this.timer = new System.Threading.Timer(callback: this.ConnectionTimerCallback, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Сброс таймера
        /// </summary>
        /// <param name="timeout">Новое значение в секундах</param>
        public void ResetTimeout(ushort timeout)
        {
            this.timeout = timeout;
            this.stopwatchStartMilliseconds = this.stopwatch.ElapsedMilliseconds;

            this.OnPropertyChanged(nameof(this.Timeout));
            this.OnPropertyChanged(nameof(this.Remaining));
        }

        #endregion

        #region IDisposable implementation
        public void Dispose()
        {
            this.logger.LogTrace("Dispose");

            this.timer?.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
