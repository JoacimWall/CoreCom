using System;
using System.Globalization;

namespace WallTec.CoreCom.Helpers
{
    public static class DateTimeConverter
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static Int32 DateTimeUtcNowToUnixTime()
        {
            
            return  (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
           
        }
        public static Int32 DateTimeUtcNowToUnixTime(DateTime dateTime)
        {
            return (Int32)(dateTime.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
           
        }
        public static DateTime UnixTimeDateTimeUtc(Int32 unixTimeStamp)
        {
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
            return dtDateTime;

        }
    }
}
