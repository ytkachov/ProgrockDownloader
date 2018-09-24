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
      MusicCollection,
      Combination
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

    protected podcast()
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

    public int correct_year()
    {
      int count = 0;
      Dictionary<string, int> aset = new Dictionary<string, int>();

      for (int phase = 0; phase < 2; phase++)
        foreach (episode ep in Episodes)
          foreach (episode_item ei in ep.Items)
          {
            string id = ei.band.ToLower() + ei.album.ToLower();
            if (phase == 0 && ei.year == 0)
              continue;

            if (phase == 0 && !aset.ContainsKey(id))
              aset.Add(id, ei.year);

            if (phase == 1 && ei.year == 0 && aset.ContainsKey(id))
            {
              ei.year = aset[id];
              count++;
            }
          }

      return count;
    }

    public int mark_repeats()
    {
      int count = 0;
      HashSet<string> sset = new HashSet<string>();
      HashSet<string> aset = new HashSet<string>();

      foreach (episode ep in Episodes)
        foreach (episode_item ei in ep.Items)
        {
          string id = ei.band.ToLower() + ei.album.ToLower() + ei.name.ToLower();
          if (!sset.Contains(id))
            sset.Add(id);
          else
          {
            ei.repeated = true;
            count++;
          }

          id = ei.band.ToLower() + ei.album.ToLower();
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
        else if (Type == PodcastType.MusicCollection)
          _page_parser = new local_folder_parser();
      }

      return _page_parser;
      throw new NotImplementedException();
    }

    public void collect_episodes(string initial_url)
    {
      IPageParser parser = get_page_parser();

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

    public void recollect_episodes()
    {
      IPageParser parser = get_page_parser();

      try
      {
        for (int i = 0; i < Episodes.Count; i++)
        {
          episode ep = Episodes[i];

          parser.Exists(ep.Url);
          string NextEpisodeUrl = parser.NextPage().Item2;  // if no new episode exists then we exit from the loop by exception

          if (NextEpisodeUrl == ep.NextEpisodeUrl)
            continue;

          episode nep = new episode() { Url = NextEpisodeUrl };
          nep.Name = parser.Exists(nep.Url);
          int ic = parser.ItemCount();

          Console.WriteLine(string.Format("{0} : {1} items", nep.Name, ic));

          nep.Items = parser.TOC();
          nep.Music = parser.DownloadLink();
          if (i != Episodes.Count - 1)
            nep.NextEpisodeUrl = parser.NextPage().Item2;  // if no new episode exists then we exit from the loop by exception

          Episodes.Insert(i + 1, nep);

          ep.NextEpisodeUrl = NextEpisodeUrl;
        }
      }
      catch (Exception e)
      {
        Console.WriteLine("\n\nException: " + e.Message + "\n\n");
      }
    }

    public void download()
    {
      {
        string datafolder = DataFolder;

        foreach (var ep in Episodes)
        {
          if (string.IsNullOrEmpty(ep.Music) || ep.Downloaded)
            continue;

          if (ep.Items.Count == 0)
            continue;

          string filename = ep.Music.Substring(ep.Music.LastIndexOf('/') + 1);
          string pathname = Path.Combine(datafolder, filename);

          if (File.Exists(pathname))
            ep.Downloaded = true;
          else
          {
            Console.WriteLine(ep.Music);

            for (int attempt = 0; attempt < 10; attempt++)
            {
              int state = 0;
              try
              {
                byte[] bytes = null;
                long totalbytes = 0;
                long recievedbytes = 0;
                DateTime lastread = DateTime.Now;

                var wc = new WebClient();

                wc.DownloadDataCompleted += delegate (object sender, DownloadDataCompletedEventArgs ea)
                {
                  state = 1;
                  if (!ea.Cancelled && (ea.Error == null))
                    bytes = ea.Result;
                };

                wc.DownloadProgressChanged += delegate (object sender, DownloadProgressChangedEventArgs ea)
                {
                  state = 2;
                  if (recievedbytes != ea.BytesReceived)
                  {
                    lastread = DateTime.Now;

                    recievedbytes = ea.BytesReceived;
                    totalbytes = ea.TotalBytesToReceive;

                  }
                };

                wc.DownloadDataAsync(new Uri(ep.Music));
                while (wc.IsBusy)
                {
                  state = 3;
                  Console.Write("\r[{2}] {0} of {1}                   ", recievedbytes.ToString("#,#", CultureInfo.InvariantCulture), totalbytes.ToString("#,#", CultureInfo.InvariantCulture), attempt);
                  Thread.Sleep(1000);

                  if (DateTime.Now - lastread > TimeSpan.FromSeconds(30))
                  {
                    wc.CancelAsync();
                    Console.WriteLine(string.Format("\r[{2}] {0} -> {1} failed                      \n", recievedbytes.ToString("#,#", CultureInfo.InvariantCulture), filename, attempt));

                    break;
                  }
                }

                if (bytes != null)
                {
                  Console.WriteLine(string.Format("\r[{2}] {0} -> {1}                       \n", bytes.Length.ToString("#,#", CultureInfo.InvariantCulture), filename, attempt));
                  File.WriteAllBytes(pathname, bytes);
                  ep.Downloaded = true;

                  break;
                }
              }
              catch (Exception e)
              {
                Console.WriteLine("\n\nException: " + e.Message + "[" + state + "]\n\n");
              }
            }
          }
        }
      }
    }

    public void extract_songs()
    {
      string datafolder = DataFolder;
      string collectionfolder = CollectionFolder;
      string picturesfolder = PicturesFolder;
      try
      {
        foreach (var ep in Episodes)
        {
          string fname = Path.Combine(datafolder, ep.Music.Substring(ep.Music.LastIndexOf('/') + 1));
          if (ep.Splitted || !ep.Downloaded || !File.Exists(fname))
            continue;

          Console.WriteLine(ep.Name);
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
            if (sdata.Length == 0)
            {
              Console.WriteLine("\nNo data for song: " + song.name + " of " + song.band + "\n");
              continue;
            }

            mp3 mp3s = new mp3(sfname, sdata);
            mp3s.SetTags(song.band, song.year, song.album, song.name, song.composer, song.FindPictures(picturesfolder));
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

    public void picturize()
    {
      foreach (var ep in Episodes)
      {
        Console.WriteLine(ep.Name);
        foreach (var song in ep.Items)
        {
          string sfname = song.GetFilePath(CollectionFolder);
          if (!File.Exists(sfname))
            continue;

          string[] pctrs = song.FindPictures(PicturesFolder);
          if (pctrs.Length == 0)
            Console.WriteLine("Pictures for " + song.album + " by " + song.band + " not found!");
          else
          {
            try
            {
              mp3 mp3s = new mp3(sfname);

              mp3s.SetPictures(pctrs);
              mp3s.Write();
            }
            catch (Exception e)
            {
              Console.WriteLine("\n\nException: " + e.Message + "\n\n");
            }
          }
        }
      }
    }

  }

  public static class ClassesExtensions
  {
    public static string NormalizeFileName(this string self)
    {

      string name = self;
      name = name.Replace(":", "--").Replace("“", "").Replace("”", "").Replace("&", "and");
      name = name.Trim(" \t.".ToCharArray());
      name = name.TrimEnd(" \t.".ToCharArray());

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
