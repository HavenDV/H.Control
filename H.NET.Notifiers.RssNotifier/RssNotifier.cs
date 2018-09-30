﻿using System;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Xml;
using H.NET.Core.Settings;

namespace H.NET.Notifiers
{
    public class RssNotifier : TimerNotifier
    {
        #region Properties

        private string Url { get; set; }

        private string LastTitle { get; set; }

        #endregion

        #region Contructors

        public RssNotifier()
        {
            AddSetting(nameof(Url), o => Url = o, o => true, string.Empty, SettingType.Path);
            //AddVariable("$rss_last_title$", () => LastTitle);
        }

        #endregion

        #region Protected methods

        protected override void OnElapsed()
        {
            if (string.IsNullOrWhiteSpace(Url))
            {
                return;
            }

            using (var reader = XmlReader.Create(Url))
            {
                var feed = SyndicationFeed.Load(reader);
                var firstItem = feed.Items.FirstOrDefault();
                var title = firstItem?.Title.Text;

                if (title == null ||
                    string.Equals(title, LastTitle, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var isFirstFeed = LastTitle == null;
                LastTitle = title;

                if (isFirstFeed)
                {
                    return;
                }

                Print("New Rss: " + title);
                //OnEvent(); TODO: required create Multi runner variables
            }

        }

        #endregion

    }
}