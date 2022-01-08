using System;
using System.Collections.Generic;
using System.Linq; //Самый православный. Буду через XML парсить
using System.Text;
using System.Xml.Linq;
//using iJSON; //Всё тлен!
//using Newtonsoft.Json; //Болт клал на этот JSON!

namespace VK_Audio
{
    public class VKMethods
    {
        public struct Audio
        {

        }

        public struct AudioObject 
        {

        }

        public static void GetAudios(Action<Dictionary<string, string>> Done) //URL, Name
        {
            Dictionary<string, string> Params = new Dictionary<string, string>();
            Params.Add("owner_id", VK.UserID);
            Params.Add("count", "1000");

            VK.API("audio.get.xml", Params, (string resp) => {

                XDocument doc = XDocument.Parse(resp); //Здесь должен быть Json, но выежнуться не получилось :C

                try //На случай выбивания эксепшна
                {
                    Dictionary<string, string> AudioList = new Dictionary<string, string>();

                    foreach(XElement e in doc.Root.Elements())
                    {
                        if (e.Name.ToString() == "audio")
                        {
                            string artist = "";
                            string title = "";
                            string url = "";

                            foreach (XElement er in e.Elements())
                            {
                                if (er.Name.ToString() == "artist") artist = er.Value.ToString();
                                if (er.Name.ToString() == "title") title = er.Value.ToString();
                                if (er.Name.ToString() == "url") url = er.Value.ToString();
                            }

                            AudioList.Add(url, string.Format("{0} - {1}.mp3", artist, title));
                        }
                    }

                    Done(AudioList);
                }
                catch(Exception e) 
                {
                    System.Windows.Forms.MessageBox.Show(e.ToString() + "\n\n" + resp, "GetAudios");
                }
            });
        }
    }
}
