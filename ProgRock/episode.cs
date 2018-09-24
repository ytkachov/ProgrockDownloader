using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace progrock
{
  [XmlRoot("Episode")]
  public class episode
  {
    [XmlAttribute]
    public string Url;

    [XmlAttribute]
    public string Name;

    [XmlAttribute]
    public string Music;

    [XmlAttribute]
    public bool Downloaded;

    [XmlAttribute]
    public string NextEpisodeUrl;

    [XmlAttribute]
    public bool Splitted;

    [XmlAttribute]
    public bool Current;

    [XmlArray]
    public List<episode_item> Items;


  }

  public static class XmlSerialization
  {
    public static TObject Deserialize<TObject>(string xml)
    {
      if (xml == null)
        return default(TObject);

      var xmlSerializer = new XmlSerializer(typeof(TObject));
      using (var stringReader = new StringReader(xml))
      {
        using (var xmlTextReader = new XmlTextReader(stringReader))
        {
          var obj = (TObject)xmlSerializer.Deserialize(xmlTextReader);
          return obj;
        }
      }
    }

    public static string Serialize<TObject>(this TObject obj)
    {
      if (obj == null)
        return "";

      try
      {
        var xmlSerializer = new XmlSerializer(typeof(TObject));
        using (var stringWriter = new StringWriter())
        {
          using (var writer = XmlWriter.Create(stringWriter, new XmlWriterSettings() {  Indent = true }))
          {
            xmlSerializer.Serialize(writer, obj);
            return stringWriter.ToString();
          }
        }
      }
      catch (Exception ex)
      {
        throw new Exception("An error occurred", ex);
      }

    }
  }

}
