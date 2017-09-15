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

    //static string initial_url = @"http://munframed.com/episode-3";
    static string initial_url = @"http://www.musicinwidescreen.com/2014/03/02/episode-537";

    static void Main(string[] args)
    {
      podcast.RootFolder = @"Y:\ProgRock\MIWS";
      string podcastfilename = "mwidescreen.xml";
      Directory.CreateDirectory(podcast.CollectionFolder);
      Directory.CreateDirectory(podcast.DataFolder);
      Directory.CreateDirectory(podcast.PicturesFolder);

      podcast p = podcast.create(podcast.PodcastType.MusicInWideScreen, podcastfilename);
      int scount = p.SongCount;
      int repeatscount = p.UniqueSongCount;
      Console.WriteLine("Unique songs: {0} of {1}", repeatscount, scount);
      p.mark_repeats();
      var parser = p.get_page_parser();
      //      p.collect_episodes(parser, initial_url);

      //Console.WriteLine("Downloading music files");
      //p.download(podcast.DataFolder);

      //Console.WriteLine("Extracting songs");
      //p.extract_songs(podcast.DataFolder, podcast.CollectionFolder, podcast.PicturesFolder);

      // string band = @"Gabriel Sucea & Axel Grassi-Havnen";
      // string album = @"Manmade Heaven & Hell";
      //string band = @"Lesoir";
      //string album = @"Luctor Et Emergo";

      //string bfolder = episode_item.BandFolder(band, podcast.PicturesFolder);
      //Directory.CreateDirectory(bfolder);

      //string bafolder = episode_item.AlbumFolder(band, album, podcast.PicturesFolder);
      //Directory.CreateDirectory(bafolder);

      //var bapictures = parser.FindPictures(band, album, true);
      //for (int i = 0; i < bapictures.Count; i++)
      //{
      //  string pn = string.Format("{0:00}.jpg", i);
      //  bapictures[i].write(Path.Combine(bafolder, pn));
      //}

      try
      {
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

      parser.Shutdown();

      string res = p.Serialize();
      using (FileStream fs = new FileStream(Path.Combine(podcast.RootFolder, podcastfilename), FileMode.Create))
      {
        using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
        {
          sw.Write(res);
        }
      }
    }
  }

}
