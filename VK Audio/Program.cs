//Следующий код является нагромождением инструкций и костылей. Программа была написана к горячем порядке, поэтому структурированию и функциональности время не уделялось.

using System;
using Threading = System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Net;
using Anvil.Net;
//using iJSON;
using WMPLib;

namespace VK_Audio
{
    public class Program
    {
        public static Form MainForm;
        public static Timer UpdateTimer = new Timer();
        public static WindowsMediaPlayer Player = new WindowsMediaPlayer();

        public static int FormWidth = 500, FormHeight = 400;
        public static string Version = "0.6";
        public static string FormTitle = string.Format("VK Audio (v {0})", Version);

        public static string MusicPath = Application.StartupPath + @"\Music";

        public static string CurrentTrack = "";

        //

        
        //

        [STAThread]
        public static void Main() 
        {
            Anvil.Net.Core.Init();

            //
            
            MainForm = UI.CreateForm(new Rect(50, 10, FormWidth, FormHeight), FormTitle);
            MainForm.MaximumSize = new Size(FormWidth, FormHeight);
            CreateUI();

            //

            System.Threading.Thread.Sleep(1000);

            InitVK();
            StartPlayer();

            //

            Application.Run(MainForm);
        }

        public static void CreateUI() 
        {
            UI.Style = CommonUI.CommonStyle;

            //

            UI.Append(UI.CreateWebBrowser(new Rect(0, 0, FormWidth, FormHeight), UI.DefaultAnchor, "WebClient"), MainForm); //Фрейм браузера

            UI.Append(UI.CreatePanel(new Rect(0, 0, FormWidth, FormHeight), UI.DefaultAnchor, "MainControls"), MainForm); //Палень с елементами плеера

            UI.Append(UI.CreatePanel(new Rect(0, 0, FormWidth, FormHeight), UI.DefaultAnchor, "SyncPanel"), MainForm); //Панель с элементами 
            UI.Controls["SyncPanel"].Visible = false;

            //Блоки плеера

            UI.Append(UI.CreatePanel(new Rect(0, 0, FormWidth, 50), UI.DefaultAnchor, "PlayerPanel"), UI.Controls["MainControls"]);
            UI.Append(UI.CreatePanel(new Rect(FormWidth - 175, 50, 160, FormHeight - 50), UI.DefaultAnchor, "ConfigPanel"), UI.Controls["MainControls"]);
            UI.Append(UI.CreatePanel(new Rect(0, 50, FormWidth - 175, FormHeight - 90), UI.DefaultAnchor, "AudioListPanel"), UI.Controls["MainControls"]);
            UI.Controls["AudioListPanel"].BackColor = Color.White;

            //Заполнение конфигурационной панели
            UI.Append(UI.CreateButton(new Rect(5, 5, 150, 40), UI.DefaultAnchor, "SyncButton", "Синхронизация", delegate {
                Stop();

                VKMethods.GetAudios((Dictionary<string, string> Audios) => {
                    DownloadAudioList(Audios);
                });

            }), UI.Controls["ConfigPanel"]);

            UI.Append(UI.CreatePanel(new Rect(0, 50, 150, UI.Controls["ConfigPanel"].Height - 50), UI.DefaultAnchor, "ConfigList"), UI.Controls["ConfigPanel"]); //Отросток для возможных конфигураций, которые мне лень реализовывать

            //Заполнение панели сонхронизации
            UI.Append(UI.CreateLabel(new Rect(5, 5, FormWidth, FormHeight), UI.DefaultAnchor, "SyncProgress", "..."), UI.Controls["SyncPanel"]);

            //Заполнение панели плеера
            UI.Append(UI.CreateButton(new Rect(5, 5, 40, 40), UI.DefaultAnchor, "PlayButton", "4", delegate { // 4 - плей, ; - пауза
                TogglePlayerState();
            }), UI.Controls["PlayerPanel"]);

            UI.Controls["PlayButton"].Font = new Font("Webdings", 20f);
            //
            Player.PlayStateChange += delegate {
                if (Player.playState == WMPPlayState.wmppsPlaying)
                {
                    UI.Controls["PlayButton"].Text = ";";
                }
                else 
                {
                    UI.Controls["PlayButton"].Text = "4";
                }
            };

            //

            UI.Append(UI.CreateLabel(new Rect(MainForm.Width - 100, 5, 100, 30), UI.DefaultAnchor, "TrackTime", "0:00"), UI.Controls["PlayerPanel"]);
            UI.Controls["TrackTime"].Font = new Font("SegoeUI", 12f);

            UI.Append(UI.CreateLabel(new Rect(50, 5, 430, 30), UI.DefaultAnchor, "TrackName", "..."), UI.Controls["PlayerPanel"]);
            UI.Controls["TrackName"].Font = new Font("SegoeUI", 12f);

            UI.Append(UI.CreatePanel(new Rect(50, 35, 430, 5), UI.DefaultAnchor, "Timeline"), UI.Controls["PlayerPanel"]);
            UI.Controls["Timeline"].BackColor = UI.Style.Button.BackColor;
        }

        public static void StartPlayer() 
        {
            if (!Directory.Exists(MusicPath)) 
            {
                Directory.CreateDirectory(MusicPath); 
            }
            //
            LoadAudioList();
            //
            Player.PlayStateChange += delegate {
                if(Player.playState == WMPPlayState.wmppsMediaEnded)
                {
                    if (CurrentTrackId < TracksNames.Length - 1) SetAudioFile(CurrentTrackId + 1);
                }

                if (Player.playState == WMPPlayState.wmppsReady)
                {
                    Play();
                }

                //MessageBox.Show(Player.playState.ToString());
            };
            //
            UpdateTimer.Interval = 10;
            UpdateTimer.Enabled = true;
            UpdateTimer.Tick += new EventHandler(OnTick);
        }

        public static void InitVK()
        {
            VK.LoadAuthData();

            if (VK.UserID == "" && VK.Token == "")//!DataPrefs.HasKey("Token")
            {
                WebBrowser WB = (WebBrowser)UI.Controls["WebClient"];
                WB.ScriptErrorsSuppressed = true;

                WB.DocumentCompleted += delegate
                {
                    string Url = WB.Url.ToString();

                    VK.ParseAuthData(Url, delegate {
                        UI.Controls["WebClient"].Visible = false;
                    });
                };

                WB.Navigate(VK.AuthURL());
            }
            else 
            {
                UI.Controls["WebClient"].Visible = false;

                //MessageBox.Show(VK.Token, VK.UserID);
            }
        }

        public static int CurrentTrackId = 0;
        public static string[] TracksPaths;
        public static string[] TracksNames;

        public static void LoadAudioList() 
        {
            UI.ControlsOf(UI.Controls["AudioListPanel"]).Clear();
            //
            if (Directory.Exists(MusicPath))
            {
                string[] Files = Directory.GetFiles(MusicPath, "*.mp3");

                TracksPaths = new string[Files.Length];
                TracksNames = new string[Files.Length];

                for (int i = 0; i < Files.Length; i++)
                {
                    string[] PathElements = Files[i].Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                    string FileName = PathElements[PathElements.Length - 1];
                    string FilePath = Files[i];

                    TracksPaths[i] = FilePath;
                    TracksNames[i] = FileName;

                    int TrackId = i;

                    UI.Append(UI.CreateButton(new Rect(5, 5 + (35 * i), 300, 30), UI.DefaultAnchor, "AudioFile" + i, FileName, delegate() {
                        SetAudioFile(TrackId);
                    }), UI.Controls["AudioListPanel"]);
                }
            }
            else 
            {
                MessageBox.Show("Music directory does not exist!");
            }
        }

        public static void DownloadAudioList(Dictionary<string, string> Audios) 
        {
            UI.Controls["MainControls"].Visible = false;
            UI.Controls["SyncPanel"].Visible = true;

            int AudioNum = Audios.Count();

            KeyValuePair<string, string>[] AudiosArray = new KeyValuePair<string, string>[AudioNum]; //Переводим словарь в позиционный массив
            
            //

            int ArrCount = 0;
            foreach (KeyValuePair<string, string> e in Audios)
            {
                AudiosArray[ArrCount] = e;
                ArrCount++;
            }

            //

            DownloadFile(0, AudiosArray);
        }

        static string ProgressText = "";
        static int ChangesCount = 0;
        static bool Downloading = false;

        public static void DownloadFile(int ArrCount, KeyValuePair<string, string>[] AudiosArray) //Рекурсивная функция
        {
            string SavePath = MusicPath + @"\" + AudiosArray[ArrCount].Value;

            if (!File.Exists(SavePath))
            {
                Threading.Thread DT = new Threading.Thread(() =>
                {
                    WebClient WC = new WebClient();
                    WC.DownloadFileCompleted += delegate
                    {
                        if (ArrCount < AudiosArray.Length - 1) 
                        { 
                            DownloadFile(ArrCount + 1, AudiosArray);
                            Downloading = true;
                        }
                        else
                        {
                            Downloading = false;
                        }
                    };

                    WC.DownloadProgressChanged += delegate
                    {
                        ProgressText = "Процесс загрузки: " + ArrCount + "/" + AudiosArray.Length + "\n" + "Операции загрузки: " + ChangesCount;

                        ChangesCount++;
                    };

                    WC.DownloadFileAsync(new Uri(AudiosArray[ArrCount].Key), SavePath);
                });

                DT.Start();

                ///

                //UI.Controls["SyncProgress"].Text = ProgressText;
            }
            else 
            {
                if (ArrCount < AudiosArray.Length - 1)
                {
                    DownloadFile(ArrCount + 1, AudiosArray);
                    Downloading = true;
                }
                else
                {
                    Downloading = false;
                }
            }
        }

        ///

        public static void SetAudioFile(int TrackId) 
        {
            CurrentTrackId = TrackId;
            CurrentTrack = TracksNames[CurrentTrackId];
            MainForm.Text = string.Format("{0} | {1}", CurrentTrack, FormTitle);
            ///
            Player.URL = TracksPaths[CurrentTrackId];
            Play();
        }

        public static void TogglePlayerState() 
        {
            if (Player.playState != WMPPlayState.wmppsPlaying)
            {
                if(Player.URL != "")
                {
                    Play();
                }
            }
            else 
            {
                Pause();
            }
        }

        public static void Play()
        {
            try
            {
                Player.controls.play();
            }
            catch
            {

            }
        }

        public static void Pause()
        {
            try
            {
                Player.controls.pause();
            }
            catch
            {

            }
        }

        public static void Stop()
        {
            try
            {
                Player.controls.stop();
            }
            catch
            {

            }
        }

        public static void OnTick(object sender, EventArgs e) 
        {
            UI.Controls["SyncProgress"].Text = ProgressText;

            UI.Controls["MainControls"].Visible = !Downloading;
            UI.Controls["SyncPanel"].Visible = Downloading;

            if(Downloading)
            {
                LoadAudioList();
            }

            //Downloading = false;
            //

            try
            {
                if (Player.playState != WMPPlayState.wmppsPlaying) return;

                double CurrentPosition = Math.Floor(Player.controls.currentPosition);
                double Duration = Math.Floor(Player.currentMedia.duration);
                double[] CurrentTime = new double[2];
                double[] FullTime = new double[2];
                ///
                CurrentTime[0] = Math.Floor(CurrentPosition / 60);
                CurrentTime[1] = CurrentPosition - (CurrentTime[0] * 60);

                FullTime[0] = Math.Floor(Duration / 60);
                FullTime[1] = Duration - (FullTime[0] * 60);
                ///
                UI.Controls["Timeline"].Width = (int)(CurrentPosition / Duration * 430);
                ///
                string CurSeconds = (CurrentTime[1].ToString().Length == 1) ? "0" + CurrentTime[1].ToString() : CurrentTime[1].ToString();
                string FullSeconds = (FullTime[1].ToString().Length == 1) ? "0" + FullTime[1].ToString() : FullTime[1].ToString();
                
                string TrackTimer = string.Format("{0}:{1}", CurrentTime[0], CurSeconds) + " | " + string.Format("{0}:{1}", FullTime[0], FullSeconds);

                UI.Controls["TrackName"].Text = CurrentTrack;
                UI.Controls["TrackTime"].Text = TrackTimer;
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString(), "");
            }
        }
    }
}
