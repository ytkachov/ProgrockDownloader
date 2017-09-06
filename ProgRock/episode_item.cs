using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace progrock
{
  [XmlRoot("EpisodeItem")]
  public class episode_item 
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
}
