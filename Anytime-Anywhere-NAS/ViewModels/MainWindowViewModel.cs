using Anytime_Anywhere_NAS.Services;
using ReactiveUI;
using System.Reactive;
using System.Threading.Tasks;

namespace Anytime_Anywhere_NAS.ViewModels
{
	public partial class MainWindowViewModel : ViewModelBase
	{
		private string _header = "Click 'Scan' to check system specs.";
		public string Header
		{
			get => _header;
			set => this.RaiseAndSetIfChanged(ref _header, value);
		}

		private NasService _nasService;

		public ReactiveCommand<Unit, Unit> ScanSystemCommand { get; }

		public MainWindowViewModel()
		{
			_nasService = new NasService();

			// Define what the command does
			ScanSystemCommand = ReactiveCommand.CreateFromTask(ScanSystemAsync);
		}

		private async Task ScanSystemAsync()
		{
			Header = "Scanning...";
			var info = _nasService.GetSystemInfo();
			Header = $"OS: {info.OS} | Cores: {info.TotalCores} | RAM: {info.TotalRamGB} GB";
		}
	}
}
