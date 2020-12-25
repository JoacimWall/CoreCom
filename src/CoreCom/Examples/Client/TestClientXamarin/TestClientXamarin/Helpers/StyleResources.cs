using System;
using Xamarin.Forms;

namespace TestClientXamarin.Helpers
{
    public class StyleResources
    {



        public static Color WhiteColor()
        {
            Application.Current.Resources.TryGetValue("WhiteColor", out object style);
            return (Color)style;

        }
        public static Color Gray100Color()
        {
            Application.Current.Resources.TryGetValue("Gray100Color", out object style);
            return (Color)style;

        }


        public static Color Primary100Color()
        {
            Application.Current.Resources.TryGetValue("Primary100Color", out object style);
            return (Color)style;

        }
        public static Color Primary200Color()
        {
            Application.Current.Resources.TryGetValue("Primary200Color", out object style);
            return (Color)style;

        }
        public static Color Primary400Color()
        {
            Application.Current.Resources.TryGetValue("Primary400Color", out object style);
            return (Color)style;

        }
        public static Color BlackColor()
        {
            Application.Current.Resources.TryGetValue("BlackColor", out object style);
            return (Color)style;

        }
        public static Color Primary800Color()
        {
            Application.Current.Resources.TryGetValue("Primary800Color", out object style);
            return (Color)style;

        }
        public static Color Primary500Color()
        {
            Application.Current.Resources.TryGetValue("Primary500Color", out object style);
            return (Color)style;

        }
        public static Color Gray300Color()
        {
            Application.Current.Resources.TryGetValue("Gray300Color", out object style);
            return (Color)style;

        }
        public static Color Gray400Color()
        {
            Application.Current.Resources.TryGetValue("Gray400Color", out object style);
            return (Color)style;

        }
        public static Color Gray700Color()
        {
            Application.Current.Resources.TryGetValue("Gray700Color", out object style);
            return (Color)style;

        }

        public static Color Gray200Color()
        {
            Application.Current.Resources.TryGetValue("Gray200Color", out object style);
            return (Color)style;

        }
        public static Color Gray900Color()
        {
            Application.Current.Resources.TryGetValue("Gray900Color", out object style);
            return (Color)style;

        }
        public static Color Red500Color()
        {
            Application.Current.Resources.TryGetValue("Red500Color", out object style);
            return (Color)style;

        }

        public static Color Gold500Color()
        {
            Application.Current.Resources.TryGetValue("Gold500Color", out object style);
            return (Color)style;

        }
        //public static Style labelBoldButtonBaseStyle()
        //{
        //    Application.Current.Resources.TryGetValue("labelBoldButtonBaseStyle", out object style);
        //    return (Style)style;

        //}


    }
}
