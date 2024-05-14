using ParkEase.ViewModel;

namespace ParkEase.Page;

public partial class SignUpPage : ContentPage
{
	public SignUpPage()
	{
		InitializeComponent();
	}

    private void ConfirmPasswordCommand(object sender, EventArgs e)
    {
        if (BindingContext is SignUpViewModel viewModel)
        {
            viewModel.ConfirmPasswordCommand.Execute(null);
        }
    }
}