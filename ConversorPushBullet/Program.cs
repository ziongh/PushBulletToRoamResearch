using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ConversorPushBullet
{
    class Program
    {
        static void Main(string[] args)
        {
            var myJsonString = File.ReadAllText("E:\\Downloads\\pushbullet.json");

            var allPushBullets = JsonConvert.DeserializeObject<PushBulletMessages>(myJsonString);


            var messagesByDay = allPushBullets.pushes.GroupBy(s => s.created.Date);

            var roamPages = new List<RoamPage>();

            foreach (var messagesOnDay in messagesByDay)
            {
                var pageName = messagesOnDay.Key.ToStringMine();

                var pageMessages =  new List<RoamMessage>();

                foreach (var message in messagesOnDay)
                {
                    if(string.IsNullOrEmpty(message.title?.Trim()) && string.IsNullOrEmpty(message.url?.Trim())) continue;
                    pageMessages.Add(new RoamMessage{
                        messageString = "#[[Quick Capture]]",
                        children = new List<RoamMessage>
                        {
                            new RoamMessage
                            {
                                messageString = $"[{message.title?.Trim()}]({message.url?.Trim()}) [[Waiting]]"
                            }
                        }
                    });
                }

                if (!pageMessages.Any())
                {
                    continue;
                }

                roamPages.Add(new RoamPage()
                {
                    title = pageName,
                    children = pageMessages
                });
            }

            var output = JsonConvert.SerializeObject(roamPages, new JsonSerializerSettings{
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore,
                Culture = CultureInfo.InvariantCulture
            });

            File.WriteAllText("E:\\Downloads\\roam.json",output);

            Console.WriteLine("OK!");
        }
    }



    public class PushBulletMessages
    {
        public List<PushBulletMessage> pushes {get;set;}
    }

    public class PushBulletMessage
    {
        public string title {get;set;}
        public string url {get;set;}
        [JsonConverter(typeof(CustomDateTimeConverter))]
        public DateTime created {get;set;}
    }


    
    public class RoamPage
    {
        public string title {get;set;}
        public List<RoamMessage> children {get;set;}
    }


    public class RoamMessage
    {
        [JsonProperty(PropertyName = "string")]
        public string messageString {get;set;}
        public List<RoamMessage> children {get;set;}
    }


    /// <summary>
    /// Custom DateTime JSON serializer/deserializer
    /// </summary>
    public class CustomDateTimeConverter :  DateTimeConverterBase
    {
        /// <summary>
        /// DateTime format
        /// </summary>
        private const string Format = "dd. MM. yyyy HH:mm";

        /// <summary>
        /// Writes value to JSON
        /// </summary>
        /// <param name="writer">JSON writer</param>
        /// <param name="value">Value to be written</param>
        /// <param name="serializer">JSON serializer</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((DateTime)value).ToString(Format));
        }

        /// <summary>
        /// Reads value from JSON
        /// </summary>
        /// <param name="reader">JSON reader</param>
        /// <param name="objectType">Target type</param>
        /// <param name="existingValue">Existing value</param>
        /// <param name="serializer">JSON serialized</param>
        /// <returns>Deserialized DateTime</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null)
            {
                return null;
            }

            var s = reader.Value.ToString();

            var datePArt = s.Split(",")[0];

            long.TryParse(datePArt, out var ticks);

            return ticks.FromUnixTime();
        }
    }


    ///<summary>
    ///</summary>
    public static class UnixDateTimeHelper
    {
        private const string InvalidUnixEpochErrorMessage = "Unix epoc starts January 1st, 1970";
        /// <summary>
        ///   Convert a long into a DateTime
        /// </summary>
        public static DateTime FromUnixTime(this Int64 self)
        {
            var ret = new DateTime(1970, 1, 1);
            return ret.AddSeconds(self);
        }

        /// <summary>
        ///   Convert a DateTime into a long
        /// </summary>
        public static Int64 ToUnixTime(this DateTime self)
        {

            if (self == DateTime.MinValue)
            {
                return 0;
            }

            var epoc = new DateTime(1970, 1, 1);
            var delta = self - epoc;

            if (delta.TotalSeconds < 0) throw new ArgumentOutOfRangeException(InvalidUnixEpochErrorMessage);

            return (long) delta.TotalSeconds;
        }


        public static string ToStringMine(this DateTime dt)
        {
            string suffix = "th";
            if (dt.Day < 10 || dt.Day > 20)
            {
                switch (dt.Day % 10)
                {
                    case 1: 
                        suffix = "st";
                        break;
                    case 2:
                        suffix = "nd";
                        break;
                    case 3:
                        suffix = "rd";
                        break;
                    default:
                        suffix = "th";
                        break;
                }
            }

            string format = $"MMM dd\"{suffix}\", yyyy";
            string s = dt.ToString(format, DateTimeFormatInfo.InvariantInfo);
            return s;
        }
    }
}
