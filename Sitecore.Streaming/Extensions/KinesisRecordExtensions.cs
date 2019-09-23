using Amazon.KinesisFirehose.Model;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Sitecore.Streaming.Extensions
{
    public static class KinesisRecordExtensions
    {
        private const string UnixNewLine = "\n";

        public static string ToJson(this object input)
        {
            return JsonConvert.SerializeObject(input);
        }

        public static string ToJsonWithNewline(this object input)
        {
            return ToJson(input) + UnixNewLine;
        }

        public static Record ToKinesisRecord(this string data)
        {
            return new Record()
            {
                Data = new MemoryStream(Encoding.UTF8.GetBytes(data))
            };
        }

        public static IEnumerable<List<Record>> SplitIntoChunks(this List<Record> kinesisRecords, int chunkSize = 500)
        {
            var processed = 0;
            var listOfChunks = new List<List<Record>>();

            while (processed < kinesisRecords.Count)
            {
                listOfChunks.Add(kinesisRecords.Skip(processed).Take(chunkSize).ToList());
                processed += chunkSize;
            }

            return listOfChunks;
        }
    }
}
