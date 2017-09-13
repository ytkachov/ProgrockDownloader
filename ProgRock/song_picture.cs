using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace progrock
{
  public class song_picture
  {
    private bool _selected;

    public Bitmap bitmap;
    public string picture_path;
    public byte [] rawdata;
    public bool selected
    {
      get
      {
        return _selected;
      }
      set
      {
        _selected = value;
        string fn = Path.GetFileNameWithoutExtension(picture_path);
        int pos = fn.LastIndexOf('#');
        if (_selected)
        {
          if (pos == -1)
            fn = fn + "#";
        }
        else
        {
          if (pos != -1)
            fn = fn.Remove(pos);
        }


        string newname = Path.Combine(Path.GetDirectoryName(picture_path), fn + Path.GetExtension(picture_path));
        if (newname != picture_path)
        {
          if (File.Exists(newname))
            File.Delete(newname);

          File.Move(picture_path, newname);
          picture_path = newname;
        }
      }
    }

    public song_picture(Bitmap picture)
    {
      bitmap = picture;
    }

    public song_picture(string path)
    {
      picture_path = path;
      selected = Path.GetFileName(picture_path).IndexOf("#") != -1;
      try
      {
        rawdata = File.ReadAllBytes(picture_path);
      }
      catch
      {
      }
    }

    public void write(string path)
    {
      picture_path = path;
      try
      {
        bitmap.Save(picture_path, ImageFormat.Jpeg);
      }
      catch (Exception e)
      {
        string m = e.Message;
      }
    }
  }
}
