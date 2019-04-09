using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using System.Web.Mvc;
using Lucene.Net.QueryParsers;
using Lucene.Net.Analysis;
using Lucene.Net.Search;
using TotalRecall.Configuration;
using System.Configuration;
using Lucene.Net.Store;
using System.IO;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Search.Spans;
using System.Security;
using Lucene.Net.Search.Highlight;

namespace TotalRecall
{
    public class SearchEngine
    {
        private readonly IndexSearcher searcher;

        public IEnumerable<Hit> Search(string query, int maxResults, out string queryParse)
        {
            queryParse = string.Empty;

            var analyzer = new StandardAnalyzer(Lucene.Net.Util.Version.LUCENE_30);

            QueryParser qp = new QueryParser(
                Lucene.Net.Util.Version.LUCENE_30,
                "contents",
                analyzer
            );
            query = QueryParser.Escape(query);
            queryParse = string.Format("\"{0}\" OR {0}*", query); 
            Query q = qp.Parse(queryParse);
            
            TopDocs top = searcher.Search(q, maxResults);
            List<Hit> result = new List<Hit>();

            foreach (var scoreDoc in top.ScoreDocs)
            {
                var doc = searcher.Doc(scoreDoc.Doc);
                string contents = doc.Get("contents");

                var scorer = new QueryScorer(q, searcher.IndexReader, "contents");
                var highlighter = new Highlighter(scorer);

                result.Add(new Hit()
                {
                    Relevance = scoreDoc.Score,
                    Title = doc.Get("title"),
                    Url = doc.Get("path"),
                    Excerpt = highlighter.GetBestFragment(analyzer, "contents", contents)
                });
            }

            return result;
        }

        public SearchEngine()
        {
            var config = (TotalRecallConfigurationSection)ConfigurationManager.GetSection("totalrecall");

            if (config == null)
            {
                config = new TotalRecallConfigurationSection()
                {
                    IndexFolder = TotalRecallConfigurationSection.DefaultIndexFolderName,
                    Optimize = TotalRecallConfigurationSection.DefaultOptimize
                };
            }

            searcher = new IndexSearcher(FSDirectory.Open(new DirectoryInfo(config.IndexFolder)), true);
        }

        public SearchEngine(string indexFolderPath)
        {
            searcher = new IndexSearcher(FSDirectory.Open(new DirectoryInfo(indexFolderPath)), true);
        }
    }
}
