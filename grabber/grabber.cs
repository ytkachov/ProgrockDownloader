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
  class grabber
  {

    static string initial_url = @"http://munframed.com/episode-3";
    //static string initial_url = @"http://www.musicinwidescreen.com/2014/03/02/episode-537";
    //static string initial_url = @"E:\";

    //static string initial_url = @"F:\Music\RUS\";

    static void Main(string[] args)
    {
      podcast.RootFolder = @"Y:\ProgRock";

      // string podcastfilename = "musr.xml";
      string podcastfilename = "munframed.xml";
      //string podcastfilename = "mwidescreen.xml";
      // string podcastfilename = "progrock.xml";
      // string podcastfilename = "sd_2.xml";

      Directory.CreateDirectory(podcast.CollectionFolder);
      Directory.CreateDirectory(podcast.DataFolder);
      Directory.CreateDirectory(podcast.PicturesFolder);

      podcast p = podcast.create(podcast.PodcastType.MusicUnframed, podcastfilename);
      int scount = p.SongCount;
      int repeatscount = p.UniqueSongCount;
      Console.WriteLine("Unique songs: {0} of {1}", repeatscount, scount);
      p.mark_repeats();
      int cy = p.correct_year();

      //collectinfo(p);
      //downloadmusicdata(p);

      //splitmusicdata(p);
      downloadpictures(p);
      //picturize(p);

      p.get_page_parser().Shutdown();
      p.save();
    }

    private static void picturize(podcast p)
    {
      p.picturize();
    }

    private static void downloadpictures(podcast p)
    {
      try
      {
        var parser = p.get_page_parser();
        var rnd = new Random(DateTime.Now.Millisecond);
        foreach (var ep in p.Episodes)
        {
          Console.WriteLine(ep.Name);

          foreach (var song in ep.Items)
          {
            var pictures = song.FindPictures(podcast.PicturesFolder, false);
            if (pictures.Length != 0)
              continue;

            Thread.Sleep(rnd.Next(1000, 5000));
            var bapictures = parser.FindPictures(song.band, song.album, true);

            if (bapictures.Count == 0)
              continue;

            string bfolder = song.BandFolder(podcast.PicturesFolder);
            Directory.CreateDirectory(bfolder);

            string bafolder = song.AlbumFolder(podcast.PicturesFolder);
            Directory.CreateDirectory(bafolder);
            for (int i = 0; i < bapictures.Count; i++)
            {
              string pn = string.Format("{0:00}.jpg", i);
              bapictures[i].write(Path.Combine(bafolder, pn));
            }
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine("\n\nException: " + e.Message + "\n\n");
      }
    }

    private static void splitmusicdata(podcast p)
    {
      Console.WriteLine("Extracting songs");
      p.extract_songs();
    }

    private static void downloadmusicdata(podcast p)
    {
      Console.WriteLine("Downloading music files");
      p.download();
    }

    private static void collectinfo(podcast p)
    {
      Console.WriteLine("collecting info");
      p.collect_episodes(initial_url);

      if (p.Type == podcast.PodcastType.MusicUnframed)
        p.recollect_episodes();
    }
  }

}
