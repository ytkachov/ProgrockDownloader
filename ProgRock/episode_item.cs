using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace progrock
{
  public class episode_item 
  {
    public string name;
    public string band;
    public string album;
    public int year;
    public string start;
    public string duration;
    public List<song_picture> pictures;
  }
}
