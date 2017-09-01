using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using munframed.model;
using System.Globalization;
using System.Windows.Markup;

namespace munframed
{
  public class NullToVisibilityConverter : MarkupExtension, IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      return value == null ? Visibility.Hidden : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
      throw new NotImplementedException();
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return this;
    }
  }

  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class Shell : Window
  {

    public Shell()
    {
      InitializeComponent();
      Dispatcher.ShutdownStarted += OnShutdownStarted;
    }

    private void OnShutdownStarted(object sender, EventArgs e)
    {
      ((MusicUnframed)DataContext).Shutdown();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      DataContext = new MusicUnframed(this);
    }
  }
}
