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
    public Bitmap bitmap;
    public string picture_path;
    public byte [] rawdata;
    public bool selected;

    public song_picture(Bitmap picture)
    {
      bitmap = picture;
    }

    public void read(string path)
    {
      picture_path = path;
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
