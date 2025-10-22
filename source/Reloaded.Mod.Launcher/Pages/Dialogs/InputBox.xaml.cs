using WindowViewModel = Reloaded.WPF.Theme.Default.WindowViewModel;

namespace Reloaded.Mod.Launcher.Pages.Dialogs
{
    public partial class InputBox : ReloadedWindow
    {
        public string Input { get; private set; }

        public InputBox(string title, string prompt, string defaultValue = "")
        {
            InitializeComponent();
            this.Title = title;
            this.PromptText.Text = prompt;
            this.InputTextBox.Text = defaultValue;

            var viewModel = (WindowViewModel)this.DataContext;
            viewModel.MinimizeButtonVisibility = Visibility.Collapsed;
            viewModel.MaximizeButtonVisibility = Visibility.Collapsed;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Input = InputTextBox.Text.Trim();
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
