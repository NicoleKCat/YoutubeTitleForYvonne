﻿using Interop.UIAutomationClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace YoutubeTitleForYvonne
{
    public partial class frmMain : Form
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        public delegate bool EnumWindowsCallback(IntPtr hwnd, int lParam);

        [DllImport("user32.dll")]
        public static extern int EnumWindows(EnumWindowsCallback Address, int y);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hwnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32")]
        private static extern int GetWindowLong(IntPtr hWnd, int index);
        [DllImport("user32")]
        private static extern int SetWindowLong(IntPtr hWnd, int index, int dwNewLong);
        [DllImport("user32")]
        private static extern int SetLayeredWindowAttributes(IntPtr hWnd, byte crey, byte alpha, int flags);

        private enum ShowWindowEnum
        {
            Hide = 0,
            ShowNormal = 1,
            ShowMinimized = 2,
            ShowMaximized = 3,
            Maximize = 3,
            ShowNormalNoActivate = 4,
            Show = 5,
            Minimize = 6,
            ShowMinNoActivate = 7,
            ShowNoActivate = 8,
            Restore = 9,
            ShowDefault = 10,
            ForceMinimized = 11
        };

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);

        [DllImport("user32.dll")]
        private static extern uint SendMessage(IntPtr hWnd, uint msg, uint wParam, uint lParam);

        bool debug = false;

        List<YoutubeWindow> youtubeWindows { get; set; } = new List<YoutubeWindow>();
        BindingSource bindingSource { get; set; } = new BindingSource();

        YoutubeWindow selectedYoutubeWindow { get; set; }
        string lastPlayingTitle { get; set; }
        string outputFileName { get; set; }
        string chromeLanguage { get; set; }
        string textSeparatorType { get; set; }
        string textSeparator { get; set; }
        int minimumTextLength { get; set; }

        public const string DefaultChromeLanguage = "English";
        public const int DefaultRefreshInterval = 2;
        public const string DefaultTextSeparatorType = "None";
        public const string DefaultTextSeparator = " ~ ";
        public const int DefaultMinimumTextLength = 40;

        private const uint WM_MOUSELEAVE = 0x02A3;

        private const int GWL_STYLE = 16;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x80000;
        private const int LWA_ALPHA = 0x2;

        private static int winLongStyle;
        private static int winLongExStyle;
        private static bool minAnimateChanged = false;

        private readonly CUIAutomation _automation;
        IUIAutomationCondition _automationCondition;
        IUIAutomationTreeWalker _automationTreeWalker;

        public static Dictionary<string, string> languages = new Dictionary<string, string>
        {
            { "af", "Afrikaans" },
            { "am", "Amharic" },
            { "ar", "Arabic" },
            { "bg", "Bulgarian" },
            { "bn", "Bengali" },
            { "ca", "Catalan" },
            { "cs", "Czech" },
            { "da", "Danish" },
            { "de", "German" },
            { "el", "Greek" },
            { "en", "English" },
            { "es", "Spanish" },
            { "et", "Estonian" },
            { "fa", "Persian" },
            { "fi", "Finnish" },
            { "fil", "Filipino" },
            { "fr", "French" },
            { "gu", "Gujarati" },
            { "he", "Hebrew" },
            { "hi", "Hindi" },
            { "hr", "Croatian" },
            { "hu", "Hungarian" },
            { "id", "Indonesian" },
            { "it", "Italian" },
            { "ja", "Japanese" },
            { "kn", "Kannada" },
            { "ko", "Korean" },
            { "lt", "Lithuanian" },
            { "lv", "Latvian" },
            { "ml", "Malayalam" },
            { "mr", "Marathi" },
            { "ms", "Malay" },
            { "nb", "Norwegian Bokmål" },
            { "nl", "Dutch" },
            { "pl", "Polish" },
            { "pt-BR", "Portuguese (Brazil)" },
            { "pt-PT", "Portuguese (Portugal)" },
            { "ro", "Romanian" },
            { "ru", "Russian" },
            { "sk", "Slovak" },
            { "sl", "Slovenian" },
            { "sr", "Serbian" },
            { "sv", "Swedish" },
            { "sw", "Swahili" },
            { "ta", "Tamil" },
            { "te", "Telugu" },
            { "th", "Thai" },
            { "tr", "Turkish" },
            { "uk", "Ukrainian" },
            { "ur", "Urdu" },
            { "vi", "Vietnamese" },
            { "zh-CN", "Chinese (China)" },
            { "zh-TW", "Chinese (Taiwan)" },
        };

        public static Dictionary<string, string> newTabString = new Dictionary<string, string>
        {
            { "Afrikaans", "Nuwe oortjie" },
            { "Amharic", "አዲስ ትር" },
            { "Arabic", "علامة تبويب جديدة" },
            { "Bulgarian", "Нов раздел" },
            { "Bengali", "নতুন ট্যাব" },
            { "Catalan", "Pestanya nova" },
            { "Czech", "Nová karta" },
            { "Danish", "Ny fane" },
            { "German", "Neuer Tab" },
            { "Greek", "Νέα καρτέλα" },
            { "English", "New tab" },
            { "Spanish", "Nueva pestaña" },
            { "Estonian", "Uus vaheleht" },
            { "Persian", "برگه جدید" },
            { "Finnish", "Uusi välilehti" },
            { "Filipino", "Bagong tab" },
            { "French", "Nouvel onglet" },
            { "Gujarati", "નવું ટૅબ" },
            { "Hebrew", "כרטיסייה חדשה" },
            { "Hindi", "नया टैब" },
            { "Croatian", "Nova kartica" },
            { "Hungarian", "Új lap" },
            { "Indonesian", "Tab baru" },
            { "Italian", "Nuova scheda" },
            { "Japanese", "新しいタブ" },
            { "Kannada", "ಹೊಸ ಟ್ಯಾಬ್" },
            { "Korean", "새 탭" },
            { "Lithuanian", "Naujas skirtukas" },
            { "Latvian", "Jauna cilne" },
            { "Malayalam", "പുതിയ ടാബ്" },
            { "Marathi", "नवीन टॅब" },
            { "Malay", "Tab baharu" },
            { "Norwegian Bokmål", "Ny fane" },
            { "Dutch", "Nieuw tabblad" },
            { "Polish", "Nowa karta" },
            { "Portuguese (Brazil)", "Nova guia" },
            { "Portuguese (Portugal)", "Novo separador" },
            { "Romanian", "Filă nouă" },
            { "Russian", "Новая вкладка" },
            { "Slovak", "Nová karta" },
            { "Slovenian", "Nov zavihek" },
            { "Serbian", "Нова картица" },
            { "Swedish", "Ny flik" },
            { "Swahili", "Kichupo kipya" },
            { "Tamil", "புதிய தாவல்" },
            { "Telugu", "కొత్త‌ ట్యాబ్" },
            { "Thai", "แท็บใหม่" },
            { "Turkish", "Yeni sekme" },
            { "Ukrainian", "Нова вкладка" },
            { "Urdu", "نیا ٹیب" },
            { "Vietnamese", "Thẻ mới" },
            { "Chinese (China)", "打开新的标签页" },
            { "Chinese (Taiwan)", "新增分頁" },
        };

        public frmMain()
        {
            InitializeComponent();

            bindingSource.DataSource = youtubeWindows;

            lstYouTubeWindows.DataSource = bindingSource;
            lstYouTubeWindows.DisplayMember = "TabName";
            lstYouTubeWindows.ValueMember = "Hwnd";
            
            outputFileName = System.IO.Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) + @"\nowplaying.txt";

            // Rather than letting UIAutomation find the new tab button by name using a filter, we'll
            // crawl through every UI element and check the name ourselves, as this stopped working.

            // We can create these once and cache them.
            _automation = new CUIAutomation();
            _automationCondition = _automation.CreateTrueCondition();
            _automationTreeWalker = _automation.CreateTreeWalker(_automationCondition);
        }

        private void btnSelectChromeWindow_Click(object sender, EventArgs e)
        {
            if (lstYouTubeWindows.SelectedItem != null)
            {
                YoutubeWindow youtubeWindow = (YoutubeWindow)lstYouTubeWindows.SelectedItem;

                selectedYoutubeWindow = youtubeWindow.Clone();

                ThreadHelperClass.SetEnabled(this, lstYouTubeWindows, false);
                ThreadHelperClass.SetEnabled(this, btnSelectChromeWindow, false);
                ThreadHelperClass.SetEnabled(this, btnStartStop, true);

                MessageBox.Show("Selected: "
                    + youtubeWindow.TabName + Environment.NewLine + Environment.NewLine
                    + "It is now OK to minimize or make the Chrome window full screen if you desire."
                    + Environment.NewLine + Environment.NewLine
                    + "If you close the selected YouTube tab or move it into a different window, come back to this application and start again by searching for YouTube tabs using button #1, then follow the process from the beginning. Moving the tab around within the same Chrome window by dragging it is OK and won't require reselecting the tab."
                    + Environment.NewLine + Environment.NewLine
                    + "You can now click \"Start monitoring tab title\" (button #3) to begin saving the video title to file. See 'Options' to specify the output filename.",
                    "Selected Chrome Window", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnStartStop_Click(object sender, EventArgs e)
        {
            if (selectedYoutubeWindow.elemTab != null)
            {
                if (!tmrUpdateCurrentlyPlaying.Enabled && btnStartStop.Text == "3. Start monitoring tab title")
                {
                    try
                    {
                        System.IO.File.WriteAllText(outputFileName, "");
                    }
                    catch
                    {
                        MessageBox.Show("The selected output file/folder:"
                            + Environment.NewLine
                            + outputFileName
                            + Environment.NewLine
                            + "is not writeable. Please change output filename under Options.",
                            "Invalid Output Filename", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    btnStartStop.Text = "3. Stop monitoring tab title";
                    lblCurrentlyPlaying.Text = "Starting...";
                    lastPlayingTitle = null;

                    // Force an update currently playing text immediately
                    bgwUpdateCurrentlyPlaying_DoWork(null, null);
                    bgwUpdateCurrentlyPlaying_RunWorkerCompleted(null, null);

                    // Start the timer to update currently playing text periodically
                    tmrUpdateCurrentlyPlaying.Enabled = true;
                }
                else
                {
                    try
                    {
                        System.IO.File.WriteAllText(outputFileName, "");
                    }
                    catch { }

                    tmrUpdateCurrentlyPlaying.Enabled = false;

                    btnStartStop.Text = "3. Start monitoring tab title";
                    lblCurrentlyPlaying.Text = "Stopped";
                    lastPlayingTitle = null;
                }
            }
            else
            {
                MessageBox.Show("Please select Chrome window from the list first and click button #2.", "Error occurred", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void btnFindYouTubeWindows_Click(object sender, EventArgs e)
        {
            youtubeWindows.Clear();
            bindingSource.ResetBindings(false);

            ThreadHelperClass.SetEnabled(this, lstYouTubeWindows, false);
            ThreadHelperClass.SetEnabled(this, btnSelectChromeWindow, false);

            //Grab all the Chrome processes
            Process[] chromeProcesses = Process.GetProcessesByName("chrome");

            //Chrome process not found
            if ((chromeProcesses.Length == 0))
            {
                MessageBox.Show(@"Google Chrome doesn't seem to be running.

Make sure Google Chrome is running with a YouTube tab open and that the Chrome window is not minimized or full screen, then try again.

It is OK to minimize or make the Chrome window full screen after this step is completed.", "No Google Chrome not found.", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            bgwFindYouTubeWindows.RunWorkerAsync();
        }

        /// <summary>
        /// This function is based on jasonfah/chrome-tab-titles on GitHub
        /// https://github.com/jasonfah/chrome-tab-titles/blob/master/c%23/ChromeTabTitles/Form1.cs
        /// </summary>
        private void showTabTitles()
        {
            //Clear our array of tab titles
            youtubeWindows.Clear();

            // Kick off our search for chrome tab titles
            EnumWindowsCallback callBackFn = new EnumWindowsCallback(Enumerator);
            EnumWindows(callBackFn, 0);
        }

        /// <summary>
        /// <para>Iterates through all visible windows - gets each chrome handle</para>
        /// <para>This function is based on jasonfah/chrome-tab-titles on GitHub
        /// https://github.com/jasonfah/chrome-tab-titles/blob/master/c%23/ChromeTabTitles/Form1.cs
        /// </para>
        /// </summary>
        private bool Enumerator(IntPtr hwnd, int lParam)
        {
            if (IsWindowVisible(hwnd))
            {
                StringBuilder sClassName = new StringBuilder(256);
                uint processId = 0;
                GetWindowThreadProcessId(hwnd, out processId);
                Process processFromID = Process.GetProcessById((int)processId);
                GetClassName(hwnd, sClassName, sClassName.Capacity);

                //Only want visible chrome windows (not any electron type apps that have chrome embedded!)
                if (((sClassName.ToString() == "Chrome_WidgetWin_1") && (processFromID.ProcessName == "chrome")))
                {
                    FindChromeTabs(hwnd);
                }
            }

            return true;
        }

        /// <summary>
        /// <para>Takes chrome window handle, searches for tabstrip by finding the parent of the "New Tab" button, then gets tab titles.</para>
        /// <para>This function is based on jasonfah/chrome-tab-titles on GitHub
        /// https://github.com/jasonfah/chrome-tab-titles/blob/master/c%23/ChromeTabTitles/Form1.cs
        /// </para>
        /// </summary>
        private void FindChromeTabs(IntPtr hwnd)
        {
            // To find the tabs we first need to locate something reliable - the 'New Tab' button

            // Get the UI element based on the Chrome window's handle
            IUIAutomationElement rootElement = _automation.ElementFromHandle(hwnd);

            // Get every child element of the Chrome window
            IUIAutomationElementArray result = rootElement.FindAll(TreeScope.TreeScope_Subtree, _automationCondition);

            // Make sure there were child elements
            if (result == null || result.Length == 0)
            {
                return;
            }

            bool foundRelevantTabInWindow = false;
            string newTabStringToFind = newTabString[chromeLanguage].ToUpper();

            // Loop through all child elements of the Chrome window
            for (int i = 0; i < result.Length; ++i)
            {
                // Get the (i)th element
                IUIAutomationElement element = result.GetElement(i);

                // If the element's name is "New Tab" (this is the name of [+] button at the end of the tab strip)
                // then we've found a child element of the tab strip.
                if (element.CurrentName != null && element.CurrentName.ToUpper() == newTabStringToFind)
                {
                    // Get the parent element, this will be the tab strip.
                    IUIAutomationElement elemTabStrip = _automationTreeWalker.GetParentElement(element);

                    // Get all child elements of the tab string. This will include tabs, as well as the [x] close button on tabs.
                    IUIAutomationElementArray tabItems = elemTabStrip.FindAll(TreeScope.TreeScope_Subtree, _automationCondition);

                    // Sanity check to make sure there were tabs. Should never happen since
                    // we already found child elements in the previous step.
                    if (tabItems == null || tabItems.Length == 0)
                    {
                        continue;
                    }

                    // Loop through all the child elements of the tab strip.
                    for (int j = 0; j < tabItems.Length; ++j)
                    {
                        // Get the (j)th element.
                        IUIAutomationElement tabItem = tabItems.GetElement(j);

                        // Get name of the element
                        string tabName = tabItem.CurrentName;

                        // If we found a tab with a YouTube video or Playlist Shuffle, then add it to our list of Chrome windows
                        // which have relevant tabs.
                        if (tabName.Contains("YouTube") || tabName.Contains("Playlist Shuffle"))
                        {
                            youtubeWindows.Add(new YoutubeWindow { TabName = tabItem.CurrentName, elemTabStrip = elemTabStrip, Hwnd = hwnd, elemTab = tabItem });
                            foundRelevantTabInWindow = true;
                        }
                    }
                }

                if (foundRelevantTabInWindow)
                {
                    break;
                }
            }
        }

        private string GetUpdatedChromeTabTitleFromTab(IUIAutomationElement elemTab)
        {
            string name = elemTab.CurrentName;

            if (!string.IsNullOrEmpty(name))
            {
                return CleanYoutubeTitle(name);
            }

            return null;
        }

        static List<Regex> filterRegex = new List<Regex>
        {
            new Regex(@"[\([［「【『]*\s*Official HD Video[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Official 4K Video[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Official Music Video[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Official Lyrics Video[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Official Lyric Video[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Original Song MV[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Original Song[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Full Song/Official Lyrics[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Full Song[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Official Video HD[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Official Video 4K[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Official Video[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Vídeo Official[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase), // Portuguese
            new Regex(@"[\([［「【『]*\s*Official Lyrics HD[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Official Lyrics 4K[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Official Lyrics[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Official Audio[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Music Video HD[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Music Video 4K[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Music Video[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Lyrics Video HD[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Lyrics Video 4K[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Lyrics Video[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Lyric Video HD[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Lyric Video 4K[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Lyric Video[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*HD Video[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*HD Audio[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*4K Video[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*4K HD[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*4KHD[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*1080p HD[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*1080pHD[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*M/V[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Video[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]*\s*Vídeo[\)\]］」】』]*", RegexOptions.Compiled | RegexOptions.IgnoreCase), // Portuguese
            new Regex(@"[\([［「【『]+\s*MV[\)\]］」】』]+", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]+\s*HD[\)\]］」】』]+", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]+\s*4K[\)\]］」】』]+", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]+\s*Official[\)\]］」】』]+", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]+\s*Audio[\)\]］」】』]+", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]+\s*Lyrics[\)\]］」】』]+", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\([［「【『]+\s*Lyric[\)\]］」】』]+", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"[\|\-]+\s*$", RegexOptions.Compiled),
            new Regex(@"^\s*[\|\-]+", RegexOptions.Compiled),
            new Regex(@"\s*-\s*YouTube Music\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase), // Removes " - YouTube Music" anywhere in the text
            new Regex(@"^\s*\d{1,3}%\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"\s*-\s*Playlist Shuffle\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex(@"\s*-\s*Google Chrome\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        };

        static Regex doubleSpaceRegex = new Regex(@"\s+", RegexOptions.Compiled);

        private string CleanYoutubeTitle(string name)
        {
            int youtubeIndex;
            int startIndex = 0;
            bool looksLikeNotificationNumber = false;

            if (!string.IsNullOrEmpty(name))
            {
                // Remove " - YouTube" from tab title
                youtubeIndex = name.IndexOf(" - YouTube");

                if (youtubeIndex != -1)
                {
                    // Remove (#) from start of tab title (occurs when you receive notifications on YouTube)
                    if (name.Length > 1 && name[0] == '(')
                    {
                        for (int i = 1; i < name.Length; ++i)
                        {
                            if (char.IsDigit(name[i]))
                            {
                                looksLikeNotificationNumber = true;
                            }
                            else if (name[i] == ')' && looksLikeNotificationNumber)
                            {
                                if (name.Length > i + 2)
                                {
                                    startIndex = i + 2;
                                }
                                break;
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    // Crop " - YouTube" and (#) notifications from title.
                    name = name.Substring(startIndex, youtubeIndex - startIndex);

                    // Filter common "music video" words
                    foreach (Regex regex in filterRegex)
                    {
                        name = regex.Replace(name, "");
                    }

                    // Convert double spaces to single space
                    name = doubleSpaceRegex.Replace(name, " ");

                    // Trim start/end of string
                    name = name.Trim();

                    // Return result
                    return name;
                }
            }

            return null;
        }

        private void bgwFindYouTubeWindows_DoWork(object sender, DoWorkEventArgs e)
        {
            ThreadHelperClass.SetVisible(this, progressBar, true);

            // Stop Timer
            tmrUpdateCurrentlyPlaying.Enabled = false;

            ThreadHelperClass.SetEnabled(this, btnFindChromeWindows, false);
            ThreadHelperClass.SetText(this, btnFindChromeWindows, "Finding YouTube tabs in non-minimized Chrome windows...");
            ThreadHelperClass.SetEnabled(this, lstYouTubeWindows, false);
            ThreadHelperClass.SetEnabled(this, btnSelectChromeWindow, false);
            ThreadHelperClass.SetText(this, btnSelectChromeWindow, "2. Select tab from list above");
            ThreadHelperClass.SetEnabled(this, btnStartStop, false);
            ThreadHelperClass.SetText(this, btnStartStop, "3. Start monitoring tab title");

            ThreadHelperClass.SetText(this, lblCurrentlyPlaying, "Stopped");

            showTabTitles();
            ThreadHelperClass.SetEnabled(this, btnFindChromeWindows, true);
            ThreadHelperClass.SetText(this, btnFindChromeWindows, "1. Find YouTube tabs in non-minimized Chrome windows");

            if (youtubeWindows.Count > 0)
            {
                ThreadHelperClass.SetEnabled(this, lstYouTubeWindows, true);
                ThreadHelperClass.SetEnabled(this, btnSelectChromeWindow, true);
            }
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawIcon(Properties.Resources.appicon, 0, 0);
        }

        private void bgwFindYouTubeWindows_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bindingSource.ResetBindings(false);
            progressBar.Visible = false;

            if (youtubeWindows.Count == 0)
            {
                MessageBox.Show(@"Google Chrome is open but could not find any non-minimized Chrome windows with YouTube tabs.

Make sure the YouTube tab is open and that the Chrome window is not minimized or full screen, then try again.

It is OK to minimize/full screen the Chrome window after this step is completed.", "No YouTube tabs were found.", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void tmrUpdateCurrentlyPlaying_Tick(object sender, EventArgs e)
        {
            tmrUpdateCurrentlyPlaying.Enabled = false;

            try
            {
                bgwUpdateCurrentlyPlaying.RunWorkerAsync();
            }
            catch (InvalidOperationException) when (debug == false)
            {
                lastPlayingTitle = null;
            }
        }

        private void bgwUpdateCurrentlyPlaying_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                ThreadHelperClass.SetVisible(this, progressBar, true);

                // Get the name of the targeted Chrome window (reflects the currently active tab name)
                string windowName = GetWindowNameFromHandle(selectedYoutubeWindow.Hwnd);

                if (ThreadHelperClass.GetText(this, btnStartStop) == "3. Stop monitoring tab title")
                {
                    // Check if the window name contains "YouTube" or "Playlist Shuffle" (case-insensitive)
                    if (string.IsNullOrEmpty(windowName) || 
                        (windowName.IndexOf("YouTube", StringComparison.OrdinalIgnoreCase) < 0 && 
                         windowName.IndexOf("Playlist Shuffle", StringComparison.OrdinalIgnoreCase) < 0))
                    {
                        // If the window name does not meet the conditions, write an empty string
                        ThreadHelperClass.SetText(this, lblCurrentlyPlaying, "No valid tab is active.");
                        System.IO.File.WriteAllText(outputFileName, "");
                        lastPlayingTitle = null;
                    }
                    else
                    {
                        // Clean the window name using the regex filters
                        string cleanedTitle = CleanYoutubeTitle(windowName);

                        if (lastPlayingTitle != cleanedTitle || ThreadHelperClass.GetText(this, lblCurrentlyPlaying) == "Starting...")
                        {
                            // If the cleaned title meets the conditions, write it to the file
                            lastPlayingTitle = cleanedTitle;
                            ThreadHelperClass.SetText(this, lblCurrentlyPlaying, cleanedTitle);

                            try
                            {
                                string textFileContent;

                                // If using space padding...
                                if (textSeparatorType == "Space Padding")
                                {
                                    // Pad end of string with spaces if text length is below minimum,
                                    // otherwise just add a single space as a separator.
                                    if (cleanedTitle.Length < minimumTextLength)
                                    {
                                        textFileContent = cleanedTitle.PadRight(minimumTextLength, ' ');
                                    }
                                    else
                                    {
                                        textFileContent = cleanedTitle + " ";
                                    }
                                }
                                else if (textSeparatorType == "Custom")
                                {
                                    textFileContent = cleanedTitle + textSeparator;
                                }
                                else
                                {
                                    textFileContent = cleanedTitle + " ";
                                }

                                System.IO.File.WriteAllText(outputFileName, textFileContent);
                            }
                            catch
                            {
                                ThreadHelperClass.SetText(this, lblCurrentlyPlaying, "Error: Output file is not writeable. Please change output filename under options.");
                                lastPlayingTitle = null;
                            }
                        }
                    }
                }
                else
                {
                    ThreadHelperClass.SetVisible(this, progressBar, false);
                }
            }
            catch when (!debug)
            {
                ThreadHelperClass.SetText(this, lblCurrentlyPlaying, "Could not get Chrome window name. Is the selected window/tab closed? Try starting again from button #1.");
                System.IO.File.WriteAllText(outputFileName, "");
                lastPlayingTitle = null;
            }
        }

        private void bgwUpdateCurrentlyPlaying_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (btnStartStop.Text == "3. Stop monitoring tab title" && btnStartStop.Enabled)
            {
                tmrUpdateCurrentlyPlaying.Enabled = true;
                progressBar.Visible = false;
                tmrUpdateCurrentlyPlaying.Start();
            }
        }

        /// <summary>
        /// <para>If window is minimized, the window will be unminimized (invisibly), the title taken and then minimized again</para>
        /// <para>Based on: https://www.codeproject.com/Articles/20651/Capturing-Minimized-Window-A-Kid-s-Trick
        /// </para>
        /// </summary>
        /// <param name="Hwnd"></param>
        /// <returns></returns>
        private string GetUpdatedChromeTabTitleFromWindow(IntPtr Hwnd)
        {
            if (Hwnd == IntPtr.Zero)
            {
                return null;
            }

            if (IsIconic(Hwnd))
            {
                if (XPAppearance.MinAnimate)
                {
                    XPAppearance.MinAnimate = false;
                    minAnimateChanged = true;
                }

                // Show main window
                winLongStyle = GetWindowLong(Hwnd, GWL_STYLE);
                winLongExStyle = GetWindowLong(Hwnd, GWL_EXSTYLE);
                SetWindowLong(Hwnd, GWL_EXSTYLE, winLongExStyle | WS_EX_LAYERED);
                SetLayeredWindowAttributes(Hwnd, 0, 1, LWA_ALPHA);

                ShowWindow(Hwnd, ShowWindowEnum.ShowNormalNoActivate);

                string updatedTabName = GetUpdatedChromeTabTitleFromTab(selectedYoutubeWindow.elemTab);

                // Paint the window
                SendMessage(Hwnd, WM_MOUSELEAVE, 0, 0);

                // Minimize the main window again
                ShowWindow(Hwnd, ShowWindowEnum.ShowMinNoActivate);

                SetWindowLong(Hwnd, GWL_EXSTYLE, winLongExStyle);
                SetWindowLong(Hwnd, GWL_STYLE, winLongStyle);

                if (minAnimateChanged)
                {
                    XPAppearance.MinAnimate = true;
                    minAnimateChanged = false;
                }

                return updatedTabName;
            }
            else
            {
                return GetUpdatedChromeTabTitleFromTab(selectedYoutubeWindow.elemTab);
            }
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            // Get Chrome Language
            chromeLanguage = ConfigurationManager.AppSettings.Get("ChromeLanguage");

            if (string.IsNullOrEmpty(chromeLanguage))
            {
                // Try to determine the language based on the user's Windows language
                CultureInfo ci = CultureInfo.InstalledUICulture;

                string foundLanguage;

                if (languages.TryGetValue(ci.TwoLetterISOLanguageName, out foundLanguage))
                {
                    chromeLanguage = foundLanguage;
                }
                else if (languages.TryGetValue(ci.Name, out foundLanguage))
                {
                    chromeLanguage = foundLanguage;
                }
                else
                {
                    chromeLanguage = DefaultChromeLanguage;
                }
            }

            // Get refresh interval
            if (int.TryParse(ConfigurationManager.AppSettings.Get("RefreshInterval"), out int refreshInterval))
            {
                tmrUpdateCurrentlyPlaying.Interval = refreshInterval * 1000;
            }
            else
            {
                tmrUpdateCurrentlyPlaying.Interval = DefaultRefreshInterval * 1000;
            }

            // Get output filename
            outputFileName = ConfigurationManager.AppSettings.Get("OutputFilename");

            if (string.IsNullOrEmpty(outputFileName))
            {
                outputFileName = System.IO.Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) + @"\nowplaying.txt";
            }

            // Get text separator type
            textSeparatorType = ConfigurationManager.AppSettings.Get("TextSeparatorType");

            if (string.IsNullOrEmpty(textSeparatorType))
            {
                textSeparatorType = DefaultTextSeparatorType;
            }

            // Get minimum text length
            if (int.TryParse(ConfigurationManager.AppSettings.Get("MinimumTextLength"), out int parsedMinimumTextLength))
            {
                minimumTextLength = parsedMinimumTextLength;
            }
            else
            {
                minimumTextLength = DefaultMinimumTextLength;
            }

            // Get text separator
            textSeparator = ConfigurationManager.AppSettings.Get("TextSeparator");

            if (textSeparator == null)
            {
                textSeparator = DefaultTextSeparator;
            }
        }

        private void btnOptions_Click(object sender, EventArgs e)
        {
            using (frmOptions form = new frmOptions())
            {
                // Set form properties...

                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    tmrUpdateCurrentlyPlaying.Interval = Convert.ToInt32(form.RefreshInterval) * 1000;
                    outputFileName = form.OutputFilename;
                    lastPlayingTitle = null; // Force file write on next update
                    textSeparatorType = form.TextSeparatorType;
                    minimumTextLength = Convert.ToInt32(form.MinimumTextLength);
                    textSeparator = form.TextSeparator;

                    // Open App.Config of executable
                    Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

                    // Add an ChromeLanguage setting.
                    config.AppSettings.Settings.Remove("ChromeLanguage");
                    config.AppSettings.Settings.Add("ChromeLanguage", form.ChromeLanguage);

                    // Add an RefreshInterval setting.
                    config.AppSettings.Settings.Remove("RefreshInterval");
                    config.AppSettings.Settings.Add("RefreshInterval", Convert.ToInt32(form.RefreshInterval).ToString());

                    // Add an OutputFilename setting.
                    config.AppSettings.Settings.Remove("OutputFilename");
                    config.AppSettings.Settings.Add("OutputFilename", form.OutputFilename);

                    // Add an TextSeparatorType setting.
                    config.AppSettings.Settings.Remove("TextSeparatorType");
                    config.AppSettings.Settings.Add("TextSeparatorType", form.TextSeparatorType);

                    // Add an MinimumTextLength setting.
                    config.AppSettings.Settings.Remove("MinimumTextLength");
                    config.AppSettings.Settings.Add("MinimumTextLength", Convert.ToInt32(form.MinimumTextLength).ToString());

                    // Add an TextSeparator setting.
                    config.AppSettings.Settings.Remove("TextSeparator");
                    config.AppSettings.Settings.Add("TextSeparator", form.TextSeparator);

                    // Save the configuration file.
                    config.Save(ConfigurationSaveMode.Modified);

                    // Force a reload of a changed section.
                    ConfigurationManager.RefreshSection("appSettings");
                }
            }
        }

        // Helper method to get the name of the Chrome window
        private string GetWindowNameFromHandle(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
            {
                return null;
            }

            StringBuilder windowText = new StringBuilder(256);
            GetWindowText(hwnd, windowText, windowText.Capacity);
            return windowText.ToString();
        }
    }
}
