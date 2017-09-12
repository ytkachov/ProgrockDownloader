using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace progrock
{
  [XmlRoot("ProgRockPodcast")]
  public partial class podcast
  {
    [XmlAttribute]
    public string Name;

    [XmlArray]
    public List<episode> Episodes;

  }

  public partial class podcast
  {
    private static string _folder;
    private static string _collectionfolder;
    private static string _datafolder;
    private static string _picturesfolder;

    private IPageParser _page_parser;

    public static string RootFolder
    {
      get
      {
        return _folder;
      }
      set
      {
        _folder = value;
        _collectionfolder = Path.Combine(_folder, "_collection");
        _picturesfolder = Path.Combine(_folder, "_pictures");
        _datafolder = Path.Combine(_folder, "_musicdata");
      }
    }

    public static string CollectionFolder
    {
      get
      {
        return _collectionfolder;
      }
    }

    public static string DataFolder
    {
      get
      {
        return _datafolder;
      }
    }

    public static string PicturesFolder
    {
      get
      {
        return _picturesfolder;
      }
    }

    private podcast()
    {

    }

    public static podcast create(string pname, string path)
    {
      podcast p;
      string ppath = Path.Combine(podcast.RootFolder, path);
      if (!File.Exists(ppath))
        p = new podcast() { Name = pname, Episodes = new List<episode>() };
      else
      {
        using (FileStream file = new FileStream(ppath, FileMode.Open, FileAccess.Read))
        {
          using (StreamReader sr = new StreamReader(file))
          {
            string xmlString = sr.ReadToEnd();
            p = XmlSerialization.Deserialize<podcast>(xmlString);
          }
        }
      }

      return p;
    }

    public IPageParser get_page_parser()
    {
      if (_page_parser == null)
        _page_parser = new munframed_page_parser();

      return _page_parser;
      throw new NotImplementedException();
    }

    public void collect_episodes(IPageParser parser, string initial_url)
    {
      try
      {
        string url = initial_url;
        while (true)
        {
          episode ep = null;
          if (Episodes.Count != 0)
          {
            ep = Episodes[Episodes.Count - 1];
            url = ep.NextEpisodeUrl;
          }

          if (string.IsNullOrEmpty(url))
            parser.Exists(ep.Url); // re-read the last episode and check if new one was added
          else
          {
            ep = new episode() { Url = url };
            ep.Name = parser.Exists(ep.Url);
            int ic = parser.ItemCount();

            Console.WriteLine(string.Format("{0} : {1} items", ep.Name, ic));

            ep.Items = parser.TOC();
            ep.Music = parser.DownloadLink();
            Episodes.Add(ep);
          }

          ep.NextEpisodeUrl = parser.NextPage().Item2;  // if no new episode exists then we exit from the loop by exception
          // break;
        }
      }
      catch (Exception e)
      {
        Console.WriteLine("\n\nException: " + e.Message + "\n\n");
      }
    }

    public void download(string datafolder)
    {
      try
      {
        var wc = new WebClient();
        foreach (var ep in Episodes)
        {
          if (string.IsNullOrEmpty(ep.Music) || ep.Downloaded)
            continue;

          string filename = ep.Music.Substring(ep.Music.LastIndexOf('/') + 1);
          string pathname = Path.Combine(datafolder, filename);

          if (!File.Exists(pathname))
          {
            Console.Write(ep.Music);
            var bytes = wc.DownloadData(ep.Music);

            Console.WriteLine(string.Format(" .. {0}B -> {1}", bytes.Length, filename));
            File.WriteAllBytes(pathname, bytes);
          }

          ep.Downloaded = true;
        }
      }
      catch (Exception e)
      {
        Console.WriteLine("\n\nException: " + e.Message + "\n\n");
      }
    }

    public void extract_songs(string datafolder, string collectionfolder, string picturesfolder)
    {
      try
      {
        foreach (var ep in Episodes)
        {
          Console.WriteLine(ep.Name);
          string fname = Path.Combine(datafolder, ep.Music.Substring(ep.Music.LastIndexOf('/') + 1));
          if (ep.Splitted || !ep.Downloaded || !File.Exists(fname))
            continue;

          DateTime epbegin = DateTime.ParseExact(ep.Items[0].start, "HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

          var mf = new mp3(fname);
          foreach (var song in ep.Items)
          {
            string band = song.band.NormalizeFileName();
            string album = song.album.NormalizeFileName();
            string name = song.name.NormalizeFileName();

            string bpath = Path.Combine(collectionfolder, band);
            if (!Directory.Exists(bpath))
              Directory.CreateDirectory(bpath);

            string apath = Path.Combine(bpath, song.year.ToString() + " - " + album);
            if (!Directory.Exists(apath))
              Directory.CreateDirectory(apath);

            DateTime sbegin = DateTime.ParseExact(song.start, @"HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
            TimeSpan start = sbegin - epbegin;
            TimeSpan end = start + TimeSpan.Parse("00:" + song.duration);

            string sfname = Path.Combine(apath, name + ".mp3");
            if (File.Exists(sfname))
              continue;

            var sdata = mf.Trim(start, end);
            mp3 mp3s = new mp3(sfname, sdata);
            mp3s.SetTags(song.band, song.year, song.album, song.name, song.FindPictures(picturesfolder));
            mp3s.Write();
          }

          ep.Splitted = true;
        }
      }
      catch (Exception e)
      {
        Console.WriteLine("\n\nException: " + e.Message + "\n\n");
      }
    }


  }

  public static class ClassesExtensions
  {
    public static string NormalizeFileName(this string self)
    {

      string name = self;
      name = name.Replace(":", "--").Replace("“", "").Replace("”", "").Replace("&", "and");

      char[] badchars = System.IO.Path.GetInvalidFileNameChars();
      while (true)
      {
        int badchar = name.IndexOfAny(badchars);
        if (badchar < 0)
          break;

        name = name.Replace(name[badchar], '_');
      }

      return name;
    }
  }

}
