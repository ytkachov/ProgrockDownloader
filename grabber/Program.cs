using progrock;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace grabber
{
  class Program
  {

    static string initial_url = @"http://munframed.com/episode-3";
    static page_parser parser;

    static void Main(string[] args)
    {
      podcast.RootFolder = @"Y:\ProgRock";
      string podcastfilename = "munframed.xml";
      Directory.CreateDirectory(podcast.CollectionFolder);
      Directory.CreateDirectory(podcast.DataFolder);
      Directory.CreateDirectory(podcast.PicturesFolder);

      podcast p = podcast.create("MusicUnframed", podcastfilename);

      parser = new page_parser();
      //p.collect_episodes(parser, initial_url);

      //Console.WriteLine("Downloading music files");
      //p.download(podcast.PicturesFolder);

      //Console.WriteLine("Extracting songs");
      //p.extract_songs(datafolder, podcast.PicturesFolder, podcast.PicturesFolder);


      try
      {
        var rnd = new Random(DateTime.Now.Millisecond);
        foreach (var ep in p.Episodes)
        {
          Console.WriteLine(ep.Name);

          foreach (var song in ep.Items)
          {
            string band = song.band.NormalizeFileName();
            string album = song.album.NormalizeFileName();

            var pictures = episode_item.FindPictures(band, album, podcast.PicturesFolder, false);
            if (pictures.Length != 0)
              continue;

            Thread.Sleep(rnd.Next(1000, 5000));
            var bapictures = parser.FindPictures(band, album, true);
            if (bapictures.Count == 0)
              continue;

            string bfolder = episode_item.BandFolder(band, podcast.PicturesFolder);
            Directory.CreateDirectory(bfolder);

            string bafolder = episode_item.AlbumFolder(band, album, podcast.PicturesFolder);
            Directory.CreateDirectory(bafolder);
            for (int i = 0; i < bapictures.Count; i++)
            {
              string ext = bapictures[i].extension;
              bapictures[i].load(Path.Combine(bafolder, string.Format("{0:00}{1}", i, ext)));
            }
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine("\n\nException: " + e.Message + "\n\n");
      }

      parser.Shutdown();

      string res = p.Serialize();
      using (FileStream fs = new FileStream(podcastfilename, FileMode.Create))
      {
        using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
        {
          sw.Write(res);
        }
      }
    }
  }

}
