using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using progrock;

namespace munframed.model
{
  class SongPicture : Notifier
  {
    private string _picture_url;
    private int _x;
    private int _y;
    private BitmapImage _picture;
    private song_picture pict;

    public SongPicture(song_picture pict)
    {
      this.pict = pict;
    }

    public string PictureUrl { get { return _picture_url; } set { _picture_url = value; RaisePropertyChanged(); } }
    public int X { get { return _x; } set { _x = value; RaisePropertyChanged(); } }
    public int Y { get { return _y; } set { _y = value; RaisePropertyChanged(); } }
    public BitmapImage Picture { get { return _picture; } set { _picture = value; RaisePropertyChanged(); } }

  }
}
