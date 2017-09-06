using progrock;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace grabber
{
  class Program
  {
    static string folder = @"Y:\ProgRock";
    static string collectionfolder;
    static string datafolder;
    static string picturesfolder;
    static string podcastfilename;

    static string initial_url = @"http://munframed.com/episode-3";
    static page_parser parser;

    static void Main(string[] args)
    {
      podcastfilename = Path.Combine(folder, "munframed.xml");
      collectionfolder = Path.Combine(folder, "_collection");
      picturesfolder = Path.Combine(folder, "_pictures");
      datafolder = Path.Combine(folder, "_musicdata");
      Directory.CreateDirectory(collectionfolder);
      Directory.CreateDirectory(datafolder);
      Directory.CreateDirectory(picturesfolder);

      podcast p = podcast.create("MusicUnframed", podcastfilename);

      parser = new page_parser();
      p.collect_episodes(parser, initial_url);

      Console.WriteLine("Downloading music files");
      p.download(datafolder);

      Console.WriteLine("Extracting songs");
      p.extract_songs(datafolder, collectionfolder, picturesfolder);

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
