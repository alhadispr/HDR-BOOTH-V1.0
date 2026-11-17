using System.Windows;
using HdrBooth.Models;

namespace HdrBooth.Views;

public partial class EditEventWindow : Window
{
    private readonly EventProfile _profile;
    public EditEventWindow(EventProfile profile)
    {
        InitializeComponent(); _profile = profile;
        NameBox.Text = profile.Name; DateBox.SelectedDate = profile.EventDate;
    }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        { MessageBox.Show("Nama acara wajib diisi.", "HDR BOOTH", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        _profile.Name = NameBox.Text.Trim(); _profile.EventDate = DateBox.SelectedDate;
        DialogResult = true;
    }
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
