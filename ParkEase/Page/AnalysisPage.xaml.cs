namespace ParkEase.Page;

public partial class AnalysisPage : ContentPage
{
	public AnalysisPage()
	{
		InitializeComponent();
		isExpanded = true;
		ToggleButton.Text = "<";
	}

	private bool isExpanded = false;
	private const double ExpandedWidth = 380; // Set this to your desired expanded width

	private async void ToggleExpander(object sender, EventArgs e)
	{
		if (isExpanded)
		{
			await CollapseAsync();
		}
		else
		{
			await ExpandAsync();
		}
		isExpanded = !isExpanded;
		ToggleButton.Text = isExpanded ? "<" : ">";
	}

	private async Task ExpandAsync()
	{
		ExpandableContent.IsVisible = true;
		var buttonAnimation = new Animation(v => ToggleButton.TranslationX = v, 0, 20);
		buttonAnimation.Commit(ToggleButton, "ButtonAnimation", 16, 250, Easing.SpringOut);
		var animation = new Animation(v => ExpandableContent.WidthRequest = v, 0, ExpandedWidth);
		animation.Commit(ExpandableContent, "ExpandAnimation", 16, 250, Easing.SpringOut);
		await Task.Delay(250);
	}

	private async Task CollapseAsync()
	{
		var buttonAnimation = new Animation(v => ToggleButton.TranslationX = v, 20, 0);
		buttonAnimation.Commit(ToggleButton, "ButtonAnimation", 16, 250, Easing.SpringIn);
		var animation = new Animation(v => ExpandableContent.WidthRequest = v, ExpandedWidth, 0);
		animation.Commit(ExpandableContent, "CollapseAnimation", 16, 250, Easing.SpringIn);
		await Task.Delay(250);
		ExpandableContent.IsVisible = false;
	}
}