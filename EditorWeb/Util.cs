using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;

namespace EditorWeb {
    public static class Util {
        public static string GetContent(this string url) {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            var response = req.GetResponse();
            var streamResponse = response.GetResponseStream();

            StreamReader streamRead = new StreamReader(streamResponse);
            char[] readBuffer = new char[256];
            int count = streamRead.Read(readBuffer, 0, 256);
            string content = "";
            while (count > 0) {
                string outputData = new string(readBuffer, 0, count);
                content += outputData;
                count = streamRead.Read(readBuffer, 0, 256);
            }
            streamRead.Close();
            streamResponse.Close();
            // Release the response object resources.
            streamResponse.Close();
            return content;
        }
    }
}