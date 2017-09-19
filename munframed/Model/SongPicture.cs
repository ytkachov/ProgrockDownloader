using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using progrock;
using System.IO;

namespace munframed.model
{
  class SongPicture : Notifier
  {
    private song_picture _data;
    private BitmapImage _picture;
    private bool _current;
    public SongPicture(song_picture data)
    {
      _data = data;
      _picture = new BitmapImage();
      using (var mem = new MemoryStream(_data.rawdata))
      {
        mem.Position = 0;
        _picture.BeginInit();
        _picture.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
        _picture.CacheOption = BitmapCacheOption.OnLoad;
        _picture.UriSource = null;
        _picture.StreamSource = mem;
        _picture.EndInit();
      }
      _picture.Freeze();

    }

    public int X { get { return (int) _picture.Width; } }
    public int Y { get { return (int) _picture.Height; } }
    public bool Selected { get { return _data.selected; } set { _data.selected = value; RaisePropertyChanged(); } }
    public bool Current { get { return _current; } set { _current = value; RaisePropertyChanged(); } }
    public BitmapImage Picture { get { return _picture; } }

  }
}
