using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using Anvil.Net;

namespace VK_Audio
{
    public class VK
    {
        public static int API_ID = 3623908;

        public static string Permissions = "notify,friends,photos,audio,video,docs,notes,pages,status,wall,groups,messages,email,notifications,stats,ads,offline";
        public static string Redirect = "https://oauth.vk.com/blank.html";
        public static string Display = "touch";
        public static string API_Version = "5.29";

        public static string API_Path = "https://api.vk.com/method/";

        //Auth Data
        public static string UserID = "";
        public static string Token = "";


        public static string AuthURL() 
        {
            return string.Format("https://oauth.vk.com/authorize?client_id={0}&scope={1}&redirect_uri={2}&display={3}&v={4}&response_type=token",
                API_ID, Permissions, Redirect, Display, API_Version);
        }

        public static void ParseAuthData(string Url, Action Done) 
        {
            if (Url.Contains(VK.Redirect) && Url.Contains("#"))
            {
                string Hash = Url.Split(new char[] { '#' })[1];
                string[] Params = Hash.Split(new char[] { '&' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string Param in Params)
                {
                    string[] PArray = Param.Split(new char[] { '=' }, StringSplitOptions.RemoveEmptyEntries);

                    if (PArray[0] == "user_id") UserID = PArray[1];
                    if (PArray[0] == "access_token") Token = PArray[1];
                }

                SaveAuthData();
                Done();
            }
        }

        public static string ParamsToURI(Dictionary<string, string> Params)
        {
            string ParamsStr = "";

            foreach(KeyValuePair<string, string> P in Params)
            {
                ParamsStr += string.Format("{0}={1}&", P.Key, P.Value);
            }

            return ParamsStr;
        }

        public static void API(string MethodName, Dictionary<string, string> Params, Action<string> Done) 
        {
            try 
            {
                string URL = string.Format("{0}{1}?{2}access_token={3}", API_Path, MethodName, ParamsToURI(Params), Token); //Перед access_token нет "&". So was planned!

                WebRequest Request = WebRequest.Create(URL);
                Request.Credentials = CredentialCache.DefaultCredentials;

                HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();

                Stream Data = Response.GetResponseStream();
                StreamReader Reader = new StreamReader(Data);

                ///

                string Output = Reader.ReadToEnd();

                Done(Output);

                ///
                Data.Close();
                Reader.Close();
                Response.Close();
            }
            catch(Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.ToString(), "VK.API");
            }
        }



        //

        public static void SaveAuthData()
        {
            if (UserID != "") DataPrefs.SetString("UserID", UserID);
            if (Token != "") DataPrefs.SetString("Token", Token);
        }

        public static void LoadAuthData()
        {
            UserID = DataPrefs.GetString("UserID", "");
            Token = DataPrefs.GetString("Token", "");
        }

        public static void ClearAuthData() 
        {
            DataPrefs.DeleteKey("UserID");
            DataPrefs.DeleteKey("Token");
        }
    }
}
