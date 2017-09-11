using System;
using System.Threading.Tasks;
using System.ComponentModel;

using OpenQA.Selenium;
using OpenQA.Selenium.Chrome; //to use googlechrome browser.
using OpenQA.Selenium.Support;
using OpenQA.Selenium.Support.UI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

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
    protected IWebDriver _driver;
    protected object _lock = new object();

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

    public List<song_picture> FindPictures(string band, string album, bool simulate = false)
    {
      if (_driver == null)
      {
        _driver = new ChromeDriver();
        _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(20);
      }

      List<song_picture> pictures = new List<song_picture>();

      string query = "https://www.google.com/search?tbm=isch&source=hp&q=";
      var words = band.Split(" ".ToCharArray());
      foreach (var w in words)
        query += w + "+";

      words = album.Split(" ".ToCharArray());
      foreach (var w in words)
        query += w + "+";

      query += "album+images";
      _driver.Navigate().GoToUrl(query);

      var picturelinks = _driver.FindElements(By.XPath("//*[@id=\"rg_s\"]/div/a"));
      List<string> hrefs = new List<string>();
      foreach (var a in picturelinks)
      {
        string href = a.GetAttribute("href");
        if (href.Contains("youtube.com") || href.Contains("photobucket.com"))
          continue;

        hrefs.Add(href);
        if (hrefs.Count == 20)
          break;
      }

      var rnd = new Random(DateTime.Now.Millisecond);
      foreach (var href in hrefs)
      {
        if (simulate)
          Thread.Sleep(rnd.Next(1000, 5000));

        _driver.Navigate().GoToUrl(href);
        var imgs = _driver.FindElements(By.ClassName("irc_mi"));
        foreach (var img in imgs)
        {
          string cl = img.GetAttribute("class");
          if (cl != "irc_mi")
            continue;

          string link = img.GetAttribute("src").ToLower();
          if (link.EndsWith("jpg") || link.EndsWith("jpeg") || link.EndsWith("png"))
          {
            var pic = new song_picture(link);
            pictures.Add(pic);
          }

          break;
        }
      }

      return pictures;
    }

    public abstract string DownloadLink();
    public abstract string Exists(string url);
    public abstract int ItemCount();
    public abstract Tuple<string, string> NextPage();
    public abstract Tuple<string, string> PrevPage();
    public abstract List<episode_item> TOC();
  }

  public class munframed_page_parser : page_parser
  {
    public override string Exists(string url)
    {
      try
      {
        if (_driver == null)
        {
          _driver = new ChromeDriver();
          _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(20);
        }

        _driver.Navigate().GoToUrl(url);
      }
      catch
      {
        throw new PageTimeoutException();
      }

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
      if (firstrow[0].Text == "Time" && firstrow[1].Text == "Band" && firstrow[2].Text == "Song")
        firstitem = 1;

      return items.Count - firstitem;
    }

    public override string DownloadLink()
    {
      var dl = _driver.findElement(By.ClassName("powerpress_link_d"));
      if (dl == null)
        throw new IncorrectPageStructureException("Download link not found");

      return dl.GetAttribute("href");
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
      if (firstrow[0].Text == "Time" && firstrow[1].Text == "Band" && firstrow[2].Text == "Song")
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