﻿using System;
using System.Globalization;

namespace WallTec.CoreCom.Sheard.Helpers
{
    public static class DateTimeConverter
    {
        public static string DateTimeUtcNow()
        {

            // ISO8601 with 3 decimal places
            return  DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", CultureInfo.InvariantCulture);
            //=> "2017-06-26T20:45:00.070Z"
        }
    }
}
