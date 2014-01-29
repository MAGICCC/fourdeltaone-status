using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Quake3
{
    static class StringEx
    {
        public static string ParseQuake3Colors(this string s)
        {
            /* MW2:
             * ^1 = red 
             * ^2 = green 
             * ^3 = yellow
             * ^4 = blue 
             * ^5 = light blue
             * ^6 = purple 
             * ^7 = white
             * ^8 is a color that changes depending what level you are on.
             *      American maps = Dark Green
             *      Russian maps = Dark red/marroon
             *      British maps = Dark Blue
             * ^9 = grey
             * ^0 = black
             */
            /* IRC:
             * 0 white
             * 1 black
             * 2 blue (navy)
             * 3 green
             * 4 red
             * 5 brown (maroon)
             * 6 purple
             * 7 orange (olive)
             * 8 yellow
             * 9 light green (lime)
             * 10 teal (a green/blue cyan)
             * 11 light cyan (cyan) (aqua)
             * 12 light blue (royal)
             * 13 pink (light purple) (fuchsia)
             * 14 grey
             * 15 light grey (silver)
             */
            s = s
                .Replace("^0", "\x03" + "01")
                .Replace("^1", "\x03" + "04")
                .Replace("^2", "\x03" + "03")
                .Replace("^3", "\x03" + "08")
                .Replace("^4", "\x03" + "02")
                .Replace("^5", "\x03" + "12")
                .Replace("^6", "\x03" + "06")
                .Replace("^7", "\x03" + "00")
                .Replace("^8", "\x03" + "99")
                .Replace("^9", "\x03" + "14")
                ;
            return s;
        }
    }
}
