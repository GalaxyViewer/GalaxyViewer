using ReactiveUI;
using System.Reactive;
using System.Threading.Tasks;

namespace GalaxyViewer.ViewModels
{
    public class MfaPromptDialogViewModel : ReactiveObject
    {
        private string _mfaCode;
        private readonly TaskCompletionSource<string> _tcs;

        public MfaPromptDialogViewModel(TaskCompletionSource<string> tcs)
        {
            _tcs = tcs;
            SubmitMfaCodeCommand = ReactiveCommand.Create(SubmitMfaCode);
        }

        public string MfaCode
        {
            get => _mfaCode;
            set => this.RaiseAndSetIfChanged(ref _mfaCode, value);
        }

        public ReactiveCommand<Unit, Unit> SubmitMfaCodeCommand { get; }

        private void SubmitMfaCode()
        {
            _tcs.SetResult(MfaCode);
        }
    }
}