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
      if (File.Exists(_filename))
        _reader = new Mp3FileReader(_filename);
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

    public Stream Trim(TimeSpan begin, TimeSpan end)
    {
      var stream = new MemoryStream();
      Mp3Frame frame;
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

    public void SetTags(string band, int year, string album, string title, string[] picturepath)
    {
      _mp3file.Tag.Artists = new string[] { band };
      _mp3file.Tag.Year = (uint)year;
      _mp3file.Tag.Album = album;
      _mp3file.Tag.Title = title;

      if (picturepath.Length != 0)
      {
        var pictures = new TagLib.Picture[picturepath.Length];
        for (int i = 0; i < pictures.Length; i++)
          pictures[i] = new TagLib.Picture(picturepath[i]);

        _mp3file.Tag.Pictures = pictures;
      }

    }
  }
}
