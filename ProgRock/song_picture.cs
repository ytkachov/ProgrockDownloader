using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace progrock
{
  public class song_picture
  {
    public string picture_url;
    public string picture_path;
    public byte [] rawdata;
    public bool selected;

    public string extension
    {
      get
      {
        string ext = "";
        int pos = picture_url.LastIndexOf('.');
        if (pos != -1)
          ext = picture_url.Substring(pos);

        return ext;
      }
    }

    public song_picture(string url)
    {
      picture_url = url;
    }

    public song_picture()
    {
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

    public void load(string path)
    {
      picture_path = path;
      try
      {
        var bytes = new WebClient().DownloadData(picture_url);
        File.WriteAllBytes(picture_path, bytes);
      }
      catch
      {
      }
    }
  }
}
