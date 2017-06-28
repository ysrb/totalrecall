﻿using System;
using System.Collections.Generic;
using System.Text;
using NCrawler;
using NCrawler.Services;
using System.Text.RegularExpressions;
using NCrawler.HtmlProcessor;
using TotalRecall.Configuration;
using System.IO;
using System.Configuration;
using log4net;
using log4net.Repository.Hierarchy;
using log4net.Appender;
using log4net.Layout;
using log4net.Core;
using NCrawler.Interfaces;

namespace TotalRecall
{
    public class SiteCrawler
    {
        public string WebsiteUrl { get; set; }
        public IConfig Config { get; set; }
        public ILogWrapper LogWrapper { get; set; }
        public Dictionary<string, string> FilterTextRules { get; set; }
        public Dictionary<string, string> FilterLinksRules { get; set; }
        public bool AdhereToRobotRules { get; set; }
        public IEnumerable<IFilter> ExcludeFilter { get; set; }
        public UriComponents UriSensitivity { get; set; }
        public int? MaximumCrawlDepth { get; set; }
        public int MaximumThreadCount { get; set; }
        public List<string> ExtraLinks { get; set; }
        public void Crawl()
        {
            using (Crawler c = new Crawler(new Uri(this.WebsiteUrl), new HtmlDocumentProcessor(FilterTextRules, FilterLinksRules), new DocumentIndexStep(this.Config, this.LogWrapper)))
            {
                this.LogWrapper.Info("Crawler started: Using " + MaximumThreadCount + " threads");

                c.AdhereToRobotRules = AdhereToRobotRules;
                c.MaximumThreadCount = MaximumThreadCount;
                c.ExcludeFilter = ExcludeFilter;
                c.UriSensitivity = UriSensitivity;
                c.MaximumCrawlDepth = MaximumCrawlDepth;
                c.Crawl();
            }
        }

        public SiteCrawler(string websiteUrl)
            : this(websiteUrl, (TotalRecallConfigurationSection)ConfigurationManager.GetSection("totalrecall"))
        { }

        public SiteCrawler(string websiteUrl, IConfig config)
            : this(websiteUrl, config, new DefaultLogWrapper("crawler", config))
        { }

        public SiteCrawler(string websiteUrl, IConfig config, ILogWrapper log)
        {
            WebsiteUrl = websiteUrl;
            Config = config;
            LogWrapper = log;
            ExcludeFilter = new[] {
                new NCrawler.Services.RegexFilter(new Regex(@"(\.jpg|\.css|\.js|\.gif|\.jpeg|\.png|\.ico|\.pdf)"))
            };
            FilterTextRules = new Dictionary<string, string>();
            FilterLinksRules = new Dictionary<string, string>();
            UriSensitivity = UriComponents.Path;
            MaximumThreadCount = System.Environment.ProcessorCount * 2;
        }
    }
}
