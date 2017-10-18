﻿using System;
using NodaTime;
using Raven.Imports.Newtonsoft.Json;
using Raven.Imports.Newtonsoft.Json.Linq;
using Raven.Imports.NodaTime.Serialization.JsonNet;

namespace Raven.Client.NodaTime.JsonConverters
{
    /// <summary>
    /// Treats an Duration as TimeSpan for json serialization purposes.
    /// </summary>
    internal class DurationConverter : NodaConverterBase<Duration>
    {
        protected override Duration ReadJsonImpl(JsonReader reader, JsonSerializer serializer)
        {
            var timeSpan = serializer.Deserialize<TimeSpan>(reader);
            return Duration.FromTimeSpan(timeSpan);
        }

        protected override void WriteJsonImpl(JsonWriter writer, Duration value, JsonSerializer serializer)
        {
            var timeSpan = value.ToTimeSpan();
            serializer.Serialize(writer, timeSpan);
        }
    }

    /// <summary>
    /// As DurationConvert but is able to read Durations stored in NodaTime format 
    /// </summary>
    internal class RelaxedDurationConverter : DurationConverter
    {
        protected override Duration ReadJsonImpl(JsonReader reader, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                var o = JObject.Load(reader);
                return o.ToObject<Duration>();
            }

            return base.ReadJsonImpl(reader, serializer);
        }
    }
}