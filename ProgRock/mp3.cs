using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace progrock
{
  public class mp3
  {
    private string _filename;
    private Mp3FileReader _reader;
    private TagLib.File _mp3file;

    public mp3(string filename)
    {
      _filename = filename;
    }

    public mp3(string filename, Stream stream)
    {
      _filename = filename;
      using (System.IO.FileStream output = new System.IO.FileStream(filename, FileMode.Create))
      {
        stream.Seek(0, SeekOrigin.Begin);
        stream.CopyTo(output);
        output.Close();
      }

      _mp3file = TagLib.File.Create(filename);
    }

    public void Write()
    {
      if (_mp3file != null)
      {
        _mp3file.Save();
        _mp3file.Dispose();
      }
    }

    public string Band
    {
      get
      {
        if (_mp3file == null)
          _mp3file = TagLib.File.Create(_filename);

        string res = "";
        foreach (var a in _mp3file.Tag.Artists)
          res += res.Length == 0 ? a : ";" + a;

        return res;
      }
    }

    public string Album
    {
      get
      {
        if (_mp3file == null)
          _mp3file = TagLib.File.Create(_filename);

        return _mp3file.Tag.Album;
      }
    }

    public string Title
    {
      get
      {
        if (_mp3file == null)
          _mp3file = TagLib.File.Create(_filename);

        return _mp3file.Tag.Title;
      }
    }

    public int Year
    {
      get
      {
        if (_mp3file == null)
          _mp3file = TagLib.File.Create(_filename);

        return (int)_mp3file.Tag.Year;
      }
    }

    public Stream Trim(TimeSpan begin, TimeSpan end)
    {
      var stream = new MemoryStream();
      Mp3Frame frame;

      if (_reader == null)
        _reader = new Mp3FileReader(_filename);

      while ((frame = _reader.ReadNextFrame()) != null)
      {
        if (_reader.CurrentTime >= begin)
        {
          if (_reader.CurrentTime > end)
            break;

          stream.Write(frame.RawData, 0, frame.RawData.Length);
        }
      }

      return stream;
    }

    public void SetTags(string band, int year, string album, string title, string composer, string[] picturepath)
    {
      if (_mp3file == null)
        _mp3file = TagLib.File.Create(_filename);

      _mp3file.Tag.Artists = new string[] { band };
      _mp3file.Tag.Performers = new string[] { band };
      _mp3file.Tag.AlbumArtists = new string[] { band };
      _mp3file.Tag.Composers = new string[] { composer };
      _mp3file.Tag.Year = (uint)year;
      _mp3file.Tag.Album = album;
      _mp3file.Tag.Title = title;

      if (picturepath.Length != 0)
      {
        int pn = new Random(DateTime.Now.Millisecond).Next(0, picturepath.Length);
        if (pn > 0)
        {
          int i = 0;
        }

        // var pictures = new TagLib.Picture[1];
        TagLib.Id3v2.AttachedPictureFrame pic = new TagLib.Id3v2.AttachedPictureFrame();
        pic.TextEncoding = TagLib.StringType.Latin1;
        pic.MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg;
        pic.Type = TagLib.PictureType.FrontCover;
        pic.Data = TagLib.ByteVector.FromPath(picturepath[pn]);

        // save picture to file
        _mp3file.Tag.Pictures = new TagLib.IPicture[1] { pic };
      }
    }

    public void SetPictures(string[] picturepath, bool update = false)
    {
      if (_mp3file == null)
        _mp3file = TagLib.File.Create(_filename);


      if (picturepath.Length != 0)
      {
        if (_mp3file.Tag.Pictures == null || _mp3file.Tag.Pictures.Length == 0 || update)
        {
          int pn = new Random(DateTime.Now.Millisecond).Next(0, picturepath.Length);

          // var pictures = new TagLib.Picture[1];
          TagLib.Id3v2.AttachedPictureFrame pic = new TagLib.Id3v2.AttachedPictureFrame();
          pic.TextEncoding = TagLib.StringType.Latin1;
          pic.MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg;
          pic.Type = TagLib.PictureType.FrontCover;
          pic.Data = TagLib.ByteVector.FromPath(picturepath[pn]);

          // save picture to file
          _mp3file.Tag.Pictures = new TagLib.IPicture[1] { pic };
        }
      }
    }
  }
}
