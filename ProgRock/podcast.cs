﻿using System;
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
    private podcast()
    {

    }

    public static podcast create(string pname, string ppath)
    {
      podcast p;
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

    public void collect_episodes(page_parser parser, string initial_url)
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

          break;
          ep.NextEpisodeUrl = parser.NextPage().Item2;  // if no new episode exists then we exit from the loop by exception
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

          Console.Write(ep.Music);
          var bytes = wc.DownloadData(ep.Music);
          string fn = ep.Music.Substring(ep.Music.LastIndexOf('/') + 1);

          Console.WriteLine(string.Format(" .. {0}B -> {1}", bytes.Length, fn));
          File.WriteAllBytes(Path.Combine(datafolder, fn), bytes);

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
    public static string[] FindPictures(this episode_item self, string foldername)
    {
      List<string> pl = new List<string>();
      for (int i = 0; i < 20; i++)
      {
        string pn = string.Format("{0} ^^ {1} ^^ {2} ^^ {3:00}", self.band.NormalizeFileName(), self.album.NormalizeFileName(), self.year, i);
        string path = Path.Combine(foldername, pn);
        foreach (var ext in new string[] { ".jpg", ".jpeg", ".png" })
        {
          string filename = path + ext;
          if (File.Exists(filename))
            pl.Add(filename);
        }
      }

      return pl.ToArray();
    }

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
