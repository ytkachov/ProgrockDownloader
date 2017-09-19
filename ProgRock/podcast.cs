using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace progrock
{
  [XmlRoot("ProgRockPodcast")]
  public partial class podcast
  {
    [XmlAttribute]
    public PodcastType Type;

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

    private string _filename;
    private IPageParser _page_parser;

    public enum PodcastType
    {
      MusicUnframed,
      MusicInWideScreen,
      MusicCollection
    }

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

    public static podcast create(PodcastType type, string filename)
    {
      podcast p;

      string ppath = Path.Combine(podcast.RootFolder, filename);
      if (!File.Exists(ppath))
        p = new podcast() { Type = type, Episodes = new List<episode>() };
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

      p._filename = filename;
      return p;
    }

    public int SongCount
    {
      get
      {
        int sc = 0;
        foreach (episode ep in Episodes)
          sc += ep.Items.Count;

        return sc;
      }
    }

    public int UniqueSongCount
    {
      get
      {
        HashSet<string> set = new HashSet<string>();

        foreach (episode ep in Episodes)
          foreach (episode_item ei in ep.Items)
          {
            string id = ei.band + ei.album + ei.name;
            if (!set.Contains(id))
              set.Add(id);
          }

        return set.Count;
      }
    }

    public void save()
    {
      string res = this.Serialize();
      using (FileStream fs = new FileStream(Path.Combine(podcast.RootFolder, _filename), FileMode.Create))
      {
        using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
        {
          sw.Write(res);
        }
      }
    }

    public int mark_repeats()
    {
      int count = 0;
      HashSet<string> sset = new HashSet<string>();
      HashSet<string> aset = new HashSet<string>();

      foreach (episode ep in Episodes)
        foreach (episode_item ei in ep.Items)
        {
          string id = ei.band + ei.album + ei.name;
          if (!sset.Contains(id))
            sset.Add(id);
          else
          {
            ei.repeated = true;
            count++;
          }

          id = ei.band + ei.album;
          if (!aset.Contains(id))
            aset.Add(id);
          else
            ei.albumrepeated = true;
        }

      return count;
    }

    public IPageParser get_page_parser()
    {
      if (_page_parser == null)
      {
        if (Type == PodcastType.MusicInWideScreen)
          _page_parser = new miws_page_parser();
        else if (Type == PodcastType.MusicUnframed)
          _page_parser = new munframed_page_parser();
      }

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
      foreach (var ep in Episodes)
      {
        try
        {

          if (string.IsNullOrEmpty(ep.Music) || ep.Downloaded)
            continue;

          if (ep.Items.Count == 0)
            continue;

          string filename = ep.Music.Substring(ep.Music.LastIndexOf('/') + 1);
          string pathname = Path.Combine(datafolder, filename);

          if (!File.Exists(pathname))
          {
            Console.WriteLine(ep.Music);

            for (int attempt = 0; attempt < 10; attempt++)
            {
              byte[] bytes = null;
              long totalbytes = 0;
              long recievedbytes = 0;
              DateTime lastread = DateTime.Now;

              var wc = new WebClient();

              wc.DownloadDataCompleted += delegate (object sender, DownloadDataCompletedEventArgs e) 
              {
                if (!e.Cancelled && !(e.Error == null))
                  bytes = e.Result;
              };

              wc.DownloadProgressChanged += delegate (object sender, DownloadProgressChangedEventArgs e) 
              {
                if (recievedbytes != e.BytesReceived)
                {
                  lastread = DateTime.Now;

                  recievedbytes = e.BytesReceived;
                  totalbytes = e.TotalBytesToReceive;

                }
              };

              wc.DownloadDataAsync(new Uri(ep.Music));
              while (wc.IsBusy)
              {
                Console.Write("\r[{2}] {0} of {1}                   ", recievedbytes.ToString("#,#", CultureInfo.InvariantCulture), totalbytes.ToString("#,#", CultureInfo.InvariantCulture), attempt);
                Thread.Sleep(1000);

                if (DateTime.Now - lastread > TimeSpan.FromSeconds(30))
                {
                  wc.CancelAsync();
                  break;
                }
              }
              
              if (bytes != null)
              {
                Console.WriteLine(string.Format("\r [{2}] {0} -> {1}                       ", bytes.Length.ToString("#,#", CultureInfo.InvariantCulture), filename, attempt));
                File.WriteAllBytes(pathname, bytes);
                ep.Downloaded = true;

                break;
              }
            }
          }
        }
        catch (Exception e)
        {
          Console.WriteLine("\n\nException: " + e.Message + "\n\n");
        }
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

          DateTime epbegin = ep.Items[0].GetStart();

          var mf = new mp3(fname);
          foreach (var song in ep.Items)
          {
            string bpath = song.BandFolder(collectionfolder);
            Directory.CreateDirectory(bpath);

            string apath = song.YearAlbumFolder(collectionfolder);
            Directory.CreateDirectory(apath);

            DateTime sbegin = song.GetStart();
            TimeSpan start = sbegin - epbegin;
            TimeSpan end = start + song.GetDuration();

            string name = song.name.NormalizeFileName();
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
