namespace TMP.Work.CommunicatorPSDTU.Common.ViewModels
{
    using System.Windows.Input;
    using PropertyChanged.SourceGenerator;

    public sealed partial class MessageViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        [Notify(set: Setter.Private)] private string? message;
        [Notify(set: Setter.Private)] private string? detailedMessage;

        public MessageViewModel(string message, string? detailedMessage, Action? onClose)
        {
            this.message = message;
            this.detailedMessage = detailedMessage;
            this.OnClose = onClose;

            this.OkCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(() => this.OnClose?.Invoke());
        }
        public Action? OnClose { get; }

        public ICommand OkCommand { get; }
    }
}
