using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace progrock
{
  [XmlRoot("EpisodeItem")]
  public partial class episode_item 
  {
    [XmlAttribute("Name")]
    public string name;

    [XmlAttribute("Band")]
    public string band;

    [XmlAttribute("Album")]
    public string album;

    [XmlAttribute("Year")]
    public int year;

    [XmlAttribute("Start")]
    public string start;

    [XmlAttribute("Duration")]
    public string duration;
  }

  public partial class episode_item
  {
    public string[] FindPictures(string rootname, bool selectedonly = true)
    {
      return FindPictures(band, album, rootname, selectedonly);
    }

    public string BandFolder(string rootname)
    {
      return BandFolder(band, rootname);
    }

    public static string BandFolder(string band, string rootname)
    {
      return Path.Combine(rootname, band.NormalizeFileName());
    }

    public string AlbumFolder(string rootname)
    {
      return AlbumFolder(band, album, rootname);
    }

    public static string AlbumFolder(string band, string album, string rootname)
    {
      return Path.Combine(BandFolder(band, rootname), album);
    }

    public static string[] FindPictures(string band, string album, string rootname, bool selectedonly)
    {
      List<string> pl = new List<string>();

      if (Directory.Exists(BandFolder(band, rootname)))
      {
        string afolder = AlbumFolder(band, album, rootname);
        if (Directory.Exists(afolder))
        {
          for (int i = 0; i < 30; i++)
          {
            string[] formats;
            formats = selectedonly ? new string[] { "{0:00^^}" } : new string[] { "{0:00^^}", "{0:00}" };

            foreach (var fmt in formats)
            {
              string pn = string.Format(fmt, i);
              string path = Path.Combine(afolder, pn);
              foreach (var ext in new string[] { ".jpg", ".jpeg", ".png" })
              {
                string filename = path + ext;
                if (File.Exists(filename))
                  pl.Add(filename);
              }
            }
          }
        }
      }

      return pl.ToArray();
    }
  }
}
