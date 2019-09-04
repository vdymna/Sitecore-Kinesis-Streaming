using Amazon.KinesisFirehose.Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sitecore.XConnect.Streaming
{
    public static class KinesisRecordExtensions
    {
        public static string ToJson(this object input)
        {
            return JsonConvert.SerializeObject(input);
        }

        public static Record ToKinesisRecord(this string data)
        {
            return new Record()
            {
                Data = new MemoryStream(Encoding.UTF8.GetBytes(data))
            };
        }
    }
}
