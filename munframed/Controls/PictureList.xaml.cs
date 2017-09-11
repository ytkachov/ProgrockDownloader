using System;
using System.Collections.Generic;
using System.Globalization;
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
using System.Windows.Markup;
using System.Globalization;
using System.Windows.Media;
using System.Windows.Data;

namespace munframed.usercontrols
{

  public class WidthToStretch : MarkupExtension, IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
      int width = (int)value;
      // return 1;
      return width < 600 ? Stretch.None : Stretch.Uniform;
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
  /// Interaction logic for PictureList.xaml
  /// </summary>
  public partial class PictureList : UserControl
  {
    public PictureList()
    {
      InitializeComponent();
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
      //EpisodeItem item = (EpisodeItem)DataContext;
    }
  }
}
