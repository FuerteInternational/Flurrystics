using System;
using System.Net;
using System.Windows;
using System.Net.NetworkInformation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Flurrystics
{
    public static class Util
    {

        public static long getCurrentTimestamp()
        {
            DateTime unixEpoch = new DateTime(1970, 1, 1);
            DateTime currentDate = DateTime.Now;
            long totalMiliSecond = (currentDate.Ticks - unixEpoch.Ticks) / 10000;
            return totalMiliSecond;
        }

        public static bool InternetIsAvailable()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                MessageBox.Show("Internet connection not available. Please try again later.");
                return false;
            }
            return true;
        }

        public static String stripOffYear(DateTime inDateTime) {
        
        String temp = inDateTime.ToShortDateString();
        String tempYear = String.Format("{0:yyyy}",inDateTime);
        temp = temp.Replace(tempYear, " ");
        char[] tempRemove =  {'-','/',' '};
        temp = temp.TrimEnd(tempRemove).TrimStart(tempRemove);
        return temp;

        }

        public static int getLabelInterval(DateTime startD,DateTime endD) {
            TimeSpan t = endD - startD;
            int result = (int)t.TotalDays / 5;
            return result;
        }

        public static int getLabelIntervalByCount(int c)
        {
            return (c / 5);
        }

        public class ExitException : Exception { }

    }
}
