using System;
using System.Threading.Tasks;
using System.ComponentModel;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome; //to use googlechrome browser.
using OpenQA.Selenium.Support;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using OpenQA.Selenium.Interactions;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;

namespace progrock
{
  public interface IPageParser
  {
    string DownloadLink();
    string Exists(string url);
    List<song_picture> FindPictures(string band, string album, bool simulate = false);
    int ItemCount();
    Tuple<string, string> NextPage();
    Tuple<string, string> PrevPage();
    void Shutdown();
    List<episode_item> TOC();
  }

  public abstract class page_parser : IPageParser
  {
    protected string _url;
    protected IWebDriver _driver;
    protected object _lock = new object();

    protected void start(string url)
    {
      _url = url;
      try
      {
        if (_driver == null)
        {
          _driver = new ChromeDriver();
          _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(60);
        }

        _driver.Navigate().GoToUrl(url);
      }
      catch
      {
        throw new PageTimeoutException();
      }
    }

    public void Shutdown()
    {
      if (_driver != null)
      {
        lock (_lock)
        {
          _driver.Close();
          _driver.Quit();
          _driver = null;
        }
      }
    }

    public virtual string DownloadLink()
    {
      var dl = _driver.findElement(By.ClassName("powerpress_link_d"));
      if (dl == null)
        throw new IncorrectPageStructureException("Download link not found");

      return dl.GetAttribute("href");
    }

    public List<song_picture> FindPictures(string band, string album, bool simulate = false)
    {
      if (_driver == null)
      {
        _driver = new ChromeDriver();
        _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(20);
      }

      List<song_picture> pictures = new List<song_picture>();

      string query = "https://www.google.com/search?tbm=isch&source=hp&q=";
      var words = band.Split(" !*'();:@&=+$,/?%#[]".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
      foreach (var w in words)
        query += w + "+";

      words = album.Split(" !*'();:@&=+$,/?%#[]".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
      foreach (var w in words)
        query += w + "+";

      query += "album+images";
      _driver.Navigate().GoToUrl(query);

      var picturelinks = _driver.FindElements(By.XPath("//*[@id='rg_s']/div/a"));
      List<string> hrefs = new List<string>();
      foreach (var a in picturelinks)
      {
        string href = a.GetAttribute("href");
        if (href.Contains("youtube.com") || href.Contains("photobucket.com"))
          continue;

        hrefs.Add(href);
        if (hrefs.Count == 5)
          break;
      }

      var selector = By.XPath("//img[@class='irc_mi'][not(@style='visibility:hidden')]");
      var rnd = new Random(DateTime.Now.Millisecond);
      foreach (var href in hrefs)
      {
        if (simulate)
          Thread.Sleep(rnd.Next(1000, 5000));

        _driver.Navigate().GoToUrl(href);
        var img = _driver.findElement(selector);

        try
        {
          if (img != null)
          {
            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementIsVisible(selector));

            ITakesScreenshot ssdriver = _driver as ITakesScreenshot;
            byte[] rawss = ((ITakesScreenshot)_driver).GetScreenshot().AsByteArray;

            Bitmap screenshot = new System.Drawing.Bitmap(new MemoryStream(rawss));
            System.Drawing.Rectangle croppedImage = new System.Drawing.Rectangle(img.Location.X, img.Location.Y, img.Size.Width, img.Size.Height);
            screenshot = screenshot.Clone(croppedImage, screenshot.PixelFormat);

            var pic = new song_picture(screenshot);
            pictures.Add(pic);
          }
        }
        catch (Exception e)
        {
        }
      }

      return pictures;
    }

    public abstract string Exists(string url);
    public abstract int ItemCount();
    public abstract Tuple<string, string> NextPage();
    public abstract Tuple<string, string> PrevPage();
    public abstract List<episode_item> TOC();
  }

  public class local_folder_parser : page_parser
  {
    string[] _subdirs;
    int _currentsubdir;

    public override string Exists(string url)
    {
      if (_subdirs != null)
        _currentsubdir++;
      else
      {
        if (!Directory.Exists(url))
          throw new WrongSiteException();

        _subdirs = Directory.GetDirectories(url);
        if (_subdirs.Length == 0)
          throw new WrongPageException();

        _currentsubdir = 0;
      }

      return "Music collection: " + Path.GetFileName(_subdirs[_currentsubdir]); 
    }

    public override int ItemCount()
    {
      string[] filePaths = Directory.GetFiles(_subdirs[_currentsubdir], "*.mp3", SearchOption.AllDirectories);
      return filePaths.Length;
    }

    public override Tuple<string, string> NextPage()
    {
      if (_currentsubdir == _subdirs.Length - 1)
        throw new LinkNotFoundException("No next episode found");

      return new Tuple<string, string>(Path.GetFileName(_subdirs[_currentsubdir + 1]), _subdirs[_currentsubdir + 1]);
    }

    public override Tuple<string, string> PrevPage()
    {
      if (_currentsubdir == 0)
        throw new LinkNotFoundException("No prev episode found");

      return new Tuple<string, string>(Path.GetFileName(_subdirs[_currentsubdir - 1]), _subdirs[_currentsubdir -1]);
    }

    public override string DownloadLink()
    {
      return "";
    }

    public override List<episode_item> TOC()
    {
      List<episode_item> res = new List<episode_item>();
      string[] filePaths = Directory.GetFiles(_subdirs[_currentsubdir], "*.mp3", SearchOption.AllDirectories);
      foreach (var fl in filePaths)
      {
        try
        {
          var mp3f = new mp3(fl);
          if (mp3f.Band != "" && mp3f.Album != "")
            res.Add(new episode_item() { band = mp3f.Band, album = mp3f.Album, name = mp3f.Title, year = mp3f.Year, filepath = fl });
        }
        catch
        { 
        }
      }

      return res;
    }
  }

   public class miws_page_parser : page_parser
  {
    public override string Exists(string url)
    {
      start(url);

      var hdr = _driver.findElement(By.XPath("/html/body/div[1]/div[1]/div/div[1]/a"));
      if (hdr == null || hdr.Text != "Music In Widescreen")
        throw new WrongSiteException();

      var oops = _driver.findElement(By.XPath("/html/body/div[2]/div/h1"));
      if (oops != null && oops.Text.Contains("Page not found"))
        throw new WrongPageException();

      return _driver.Title;
    }

    public override int ItemCount()
    {
      var tbl = _driver.findElement(By.XPath("//*[@class='block-content post-content']/table"));
      if (tbl == null)
        return 0;

      var items = tbl.FindElements(By.TagName("tr"));
      return items.Count - 1;
    }

    public override Tuple<string, string> PrevPage()
    {
      throw new LinkNotFoundException("No previous episode found");
    }

    public override Tuple<string, string> NextPage()
    {
      string[] urlparts = _url.Split("/-".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
      int year = int.Parse(urlparts[2]);
      int month = int.Parse(urlparts[3]);
      int day = int.Parse(urlparts[4]);

      DateTime dt = new DateTime(year, month, day);
      dt = dt.AddDays(7);
      if (dt > DateTime.Now)
        throw new LinkNotFoundException("No next episode found");

      string text = "", link = "";
      foreach (int d in new int[] { 0, 1, -1, 7, 6, 8, 14, 13, 15, 21, 20, 22 })
      {
        DateTime dtt = dt.AddDays(d);

        link = string.Format("{0}//{1}/{2}/{3}/{4}", urlparts[0], urlparts[1], dtt.Year, dtt.Month, dtt.Day);
        _driver.Navigate().GoToUrl(link);
        var a = _driver.findElement(By.XPath("//h3[@class='post-title']/a"));
        if (a != null)
        {
          link = a.GetAttribute("href");
          text = a.Text;
          break;
        }
      }

      return new Tuple<string, string>(text, link);
    }


    public override List<episode_item> TOC()
    {
      List<episode_item> toc = new List<episode_item>();

      var tbls = _driver.FindElements(By.XPath("//*[@class='block-content post-content']/table"));
      if (tbls.Count == 0)
        return toc;

      var tbl = tbls[tbls.Count - 1];

      var items = tbl.FindElements(By.TagName("tr"));

      Tuple<int, string[]> t1 = new Tuple<int, string[]>(0, new string[] { "Air Time", "Band", "Song", "Song Time", "Album", "Year", "Composer", "Label" });
      Tuple<int, string[]> t2 = new Tuple<int, string[]>(1, new string[] { "Air Time", "Band", "Song", "Song Time", "Album", "Year", "Composer", "Listeners" });
      Tuple<int, string[]> t3 = new Tuple<int, string[]>(-2, new string[] { "Song", "Length", "Album", "Year", "Band", "Label", "Listeners" });
      Tuple<int, string[]> t4 = new Tuple<int, string[]>(0, new string[] { "Air Time", "Band", "Song", "Song Time", "Album", "Year", "Composer", "Label", "Listeners" });
      Tuple<int, string[]> t5 = new Tuple<int, string[]>(0, new string[] { "Time Aired", "Band", "Song", "", "Album", "Year", "Composer", "Label", "Listeners" });
      Tuple<int, string[]> t6 = new Tuple<int, string[]>(0, new string[] { "Time Aired", "Band", "Song", "", "Album", "Year", "Composer", "Label", "" });
      Tuple<int, string[]> t7 = new Tuple<int, string[]>(0, new string[] { "Air Time", "Band", "Song", "Song Time", "Album", "Year", "Composer", "Label", "" });
      Tuple<int, string[]> t8 = new Tuple<int, string[]>(0, new string[] { "Air Time", "Band", "Song", "Song Time", "Album", "Year", "Composer", "Lable", "" });
      Tuple<int, string[]> t9 = new Tuple<int, string[]>(0, new string[] { "Air Time", "Band", "Song", "Song Time", "Album", "Year", "Composer", "", "" });
      Tuple<int, string[]> tA = new Tuple<int, string[]>(0, new string[] { "Air Time", "Band", "Song", "Song Time", "Album", "Year", "Composer", "Lable", "Listeners" });
      Tuple<int, string[]> tB = new Tuple<int, string[]>(2, new string[] { "Song", "Artist", "Song", "Time", "Album", "Year", "Label", "Label" });
      Tuple<int, string[]> tC = new Tuple<int, string[]>(-2, new string[] { "Time", "Band", "Song", "Year", "Composer", "Label", "Listeners" });
      Tuple<int, string[]> tD = new Tuple<int, string[]>(-3, new string[] { "Time", "Band", "Song", "Album", "Year", "Composer", "Label", "Listeners" });
      Tuple<int, string[]> tE = new Tuple<int, string[]>(-3, new string[] { "Time", "Band", "Song", "Album", "Year", "Composer", "Label", "" });
      Tuple<int, string[]> tF = new Tuple<int, string[]>( 0, new string[] { "Time", "Band", "Song", "Time", "Album", "Year", "Composer", "Label", "" });
      Tuple<int, string[]> tG = new Tuple<int, string[]>( 0, new string[] { "Time", "Band", "Song", "Time", "Album", "Year", "Composer", "Label", "Listeners" });
      Tuple<int, string[]> tH = new Tuple<int, string[]>( 0, new string[] { "Time", "Band", "Song", "Time", "Album", "Year", "Composer", "Label" });
      Tuple<int, string[]> tI = new Tuple<int, string[]>( 0, new string[] { "Time", "Band", "Song", "Time", "Album", "Year", "Composer", "", "" });
      Tuple<int, string[]> tJ = new Tuple<int, string[]>( 0, new string[] { "Time", "Band", "Song", "Time", "Album", "", "", "", "" });
      Tuple<int, string[]> tK = new Tuple<int, string[]>( 0, new string[] { "Time", "Band", "Song", "Time", "Album", "Year", "Composer", "Listeners", "" });
      Tuple<int, string[]> tL = new Tuple<int, string[]>( 0, new string[] { "Time", "Band", "Song", "Time", "Album", "Year", "", "", "" });
      Tuple<int, string[]> tM = new Tuple<int, string[]>(0, new string[] { "Time", "Band", "Song", "Time", "Album", "Year", "Composer", "Label", "", "" });
      Tuple<int, string[]> tN = new Tuple<int, string[]>(0, new string[] { "Band", "Album", "Year", "Composer", "", "", "", "", "", "" });
      Tuple<int, string[]> tO = new Tuple<int, string[]>(0, new string[] { "Time", "Artist", "Title", "Duration", "Album", "Year", "Composer", "Label", "" });

      Tuple<int, string[]>[] types = new Tuple<int, string[]>[] { t1, t2, t3, t4, t5, t6, t7, t8, t9, tA, tB, tC, tD, tE, tF, tG, tH, tI, tJ, tK, tL, tM, tN, tO };
      var th = items[0].FindElements(By.TagName("td"));
      string[] ht = new string[th.Count];
      for (int k = 0; k < th.Count; k++)
        ht[k] = th[k].Text;

      int type = -1;
      foreach (var t in types)
      {
        if (ht.Length != t.Item2.Length)
          continue;

        int c;
        for (c = 0; c < ht.Length; c++)
          if (ht[c] != t.Item2[c])
            break;

        if (c == ht.Length)
        {
          type = t.Item1;
          break;
        }
      }

      int start = 1;
      // check if this is just table without header
      if (ht[0].isHMS() && ht[3].isMS())
      {
        type = 0;
        start = 0;
      }

      if (type < 0)
        return toc;

      for (int i = start; i < items.Count; i++)
      {
        var tds = items[i].FindElements(By.TagName("td"));
        if (tds.Count < 6)
          continue;

        var ei = new episode_item()
        {
          start = tds[0].Text,
          band = tds[1].Text,
          name = tds[2].Text,
          duration = tds[3].Text,
          album = tds[4].Text,
        };

        if (!ei.start.isHMS() || !ei.duration.isMS())
          continue;

        if (ei.band == "miwshowopen" || ei.name == "miwshowopen" || ei.name == "Musicinwidescreen ID1")
          continue;

        Int32 year = 0;
        Int32.TryParse(tds[5].Text, out year);
        ei.year = year;

        if (type == 0 && tds.Count > 7)
        {
          ei.composer = tds[6].Text;
          ei.label = tds[7].Text;
        }
        else if (type == 1 && tds.Count > 6)
        {
          ei.composer = tds[6].Text;
        }
        else if (type == 2 && tds.Count > 6)
        {
          ei.label = tds[6].Text;
        }

        toc.Add(ei);
      }

      return toc;
    }
  }

  public class munframed_page_parser : page_parser
  {
    public override string Exists(string url)
    {
      start(url);

      var hdr = _driver.findElement(By.XPath("//*[@id=\"masthead\"]/div/div/h1/a"));
      if (hdr == null || hdr.Text != "Music Unframed")
        throw new WrongSiteException();

      var oops = _driver.findElement(By.XPath("//*[@id=\"main\"]/section/header/h1"));
      if (oops != null && oops.Text == "Oops! That page can’t be found.")
        throw new WrongPageException();

      return _driver.Title;
    }

    public override int ItemCount()
    {
      var tbl = _driver.findElement(By.XPath("//*[@id=\"dslc-theme-content-inner\"]/table"));
      if (tbl == null)
        throw new IncorrectPageStructureException("Table of contents not found");

      var items = tbl.FindElements(By.TagName("tr"));
      var firstrow = items[0].FindElements(By.TagName("td"));
      int firstitem = 0;
      if (firstrow[0].Text == "Time" && firstrow[1].Text == "Band" && (firstrow[2].Text == "Song" || firstrow[2].Text == "Name"))
        firstitem = 1;

      return items.Count - firstitem;
    }

    public override Tuple<string, string> PrevPage()
    {
      var link = _driver.findElement(By.ClassName("nav-previous")).findElement(By.XPath("./a"));
      if (link == null)
        throw new LinkNotFoundException("No previous episode found");

      return new Tuple<string, string>(link.Text, link.GetAttribute("href"));
    }

    public override Tuple<string, string> NextPage()
    {
      var link = _driver.findElement(By.ClassName("nav-next")).findElement(By.XPath("./a"));
      if (link == null)
        throw new LinkNotFoundException("No next episode found");

      return new Tuple<string, string>(link.Text, link.GetAttribute("href"));
    }


    public override List<episode_item> TOC()
    {
      List<episode_item> toc = new List<episode_item>();

      var tbl = _driver.findElement(By.XPath("//*[@id=\"dslc-theme-content-inner\"]/table"));
      if (tbl == null)
        throw new IncorrectPageStructureException("Table of contents not found");

      var items = tbl.FindElements(By.TagName("tr"));

      // check if table is in old format
      var firstrow = items[0].FindElements(By.TagName("td"));
      int firstitem = 0;
      if (firstrow[0].Text == "Time" && firstrow[1].Text == "Band" && (firstrow[2].Text == "Song" || firstrow[2].Text == "Name"))
        firstitem = 1;

      for (int i = firstitem; i < items.Count; i++)
      {
        var tds = items[i].FindElements(By.TagName("td"));
        var ei = new episode_item()
        {
          start = tds[0].Text,
          band = tds[1].Text,
          name = tds[2].Text,
        };

        if (tds.Count == 4)
        {
          ei.duration = "";
          ei.album = tds[3].Text;
          ei.year = 0;
        }
        else
        {
          ei.duration = tds[3].Text;
          ei.album = tds[4].Text;

          Int32 year = 0;
          Int32.TryParse(tds[5].Text, out year);
          ei.year = year;
        }

        toc.Add(ei);
      }

      return toc;
    }

  }

  public class WrongSiteException : Exception { public WrongSiteException() : base("URL provided is not Music Unframed Site") { } }
  public class WrongPageException : Exception { public WrongPageException() : base("Page not found on the site") { } }
  public class PageTimeoutException : Exception { public PageTimeoutException() : base("Can't load the requested page") { } }
  public class LinkNotFoundException : Exception { public LinkNotFoundException(string msg) : base(msg) { } }
  public class IncorrectPageStructureException : Exception { public IncorrectPageStructureException(string msg) : base(msg) { } }


  public static class SeleniumExtensions
  {
    public static bool isHMS(this string self)
    {
      try
      {
      var substrings = self.Split(":".ToCharArray());
      if (substrings.Length == 3 && int.Parse(substrings[0]) < 24 && int.Parse(substrings[1]) < 60 && int.Parse(substrings[2]) < 60)
        return true;
      }
      catch
      {

      }

      return false;
    }

    public static bool isMS(this string self)
    {
      try
      {
        var substrings = self.Split(":".ToCharArray());
        if (substrings.Length == 2 && int.Parse(substrings[0]) < 60 && int.Parse(substrings[1]) < 60)
          return true;
        else if (substrings.Length == 3 && int.Parse(substrings[0]) < 60 && int.Parse(substrings[1]) < 60 && int.Parse(substrings[2]) == 0)
          return true;
      }
      catch
      {

      }

      return false;
    }

    public static IWebElement findElement(this ISearchContext self, By by)
    {
      if (self == null)
        return null;

      IWebElement el = null;
      try
      {
        el = self.FindElement(by);
      }
      catch
      {
      }

      return el;
    }
  }

}