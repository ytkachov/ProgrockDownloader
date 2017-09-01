using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace progrock
{
  public class song_picture
  {
    protected string _picture_url;
    protected string _picture_path;

    public song_picture(string url)
    {
      _picture_url = url;
    }

    public void load(string path)
    {
      _picture_path = path;
      try
      {
        var bytes = new WebClient().DownloadData(_picture_url);
        File.WriteAllBytes(_picture_path, bytes);
      }
      catch
      {
      }
    }
  }
}
