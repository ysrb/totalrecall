using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;
using Lucene.Net.Analysis;

namespace TotalRecall
{
    class DocumentRepository : IDisposable
    {
        private IndexWriter index;
        private IndexReader reader;
        private IndexSearcher searcher;
        private bool finalized;
        private ILogWrapper log;

        private static readonly DateTime epoch = new DateTime(1970, 1, 1);

        public Document Find(string id)
        {
            TermQuery query = new TermQuery(new Term("path", id));
            TopDocs docs = searcher.Search(query, null, 1);

            if (docs.TotalHits >= 1)
            {
                return searcher.Doc(docs.ScoreDocs[0].Doc);
            }
            return null;
        }

        public void Optimize()
        {
            index.Optimize();
        }

        public void Delete(string id)
        {
            index.DeleteDocuments(new Term("path", id));
            log.Info("Deleting document [" + id + "] from index");
        }

        public void AddUpdate(string id, string title, string contents, DateTime lastModified)
        {
            log.Info("Document ID " + id + " in AddUpdate.");
            Document doc = Find(id);

            if (doc != null)
            {
                log.Info("Document ID " + id + " in AddUpdate. doc is not null");
                if ((long)(lastModified - epoch).TotalMilliseconds < DateTools.StringToTime(doc.GetField("modified").StringValue))
                {
                    log.Info("Document ID " + id + " already exists in the index and doesn't need to be updated.");
                    return;
                }

                Delete(doc.Get("path"));
            }

            doc = new Document();
            doc.Add(new Field("path", id, Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("title", title, Field.Store.YES, Field.Index.ANALYZED));
            doc.Add(new Field("contents", contents, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
            doc.Add(new Field("modified", DateTools.TimeToString((long)(DateTime.Now - epoch).TotalMilliseconds, DateTools.Resolution.MINUTE), Field.Store.YES, Field.Index.NOT_ANALYZED));
            log.Info("Document ID " + id + " in AddUpdate doc added");
            this.index.AddDocument(doc);
            log.Info("Document ID " + id + " in AddUpdate index adddocument");
        }

        public DocumentRepository(IndexWriter index, ILogWrapper log)
        {
            this.index = index;
            this.reader = this.index.GetReader();
            this.searcher = new IndexSearcher(this.reader);
            this.log = log;
        }

        ~DocumentRepository()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!finalized)
            {
                index.Commit();
                //reader.Close();
                reader.Dispose();
                //searcher.Close();
                searcher.Dispose();
                finalized = true;
                GC.SuppressFinalize(this);
            }
        }
    }
}
