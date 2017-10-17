using System;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace AzureSearchTextAnalytics
{
    class Program
    {
        private static string TextAnalyticsAPIKey = "Text Analytics Key";     // Learn more here: https://azure.microsoft.com/en-us/documentation/articles/machine-learning-apps-text-analytics/
        private static int SentencesToSummarize = 3;

        static string searchServiceName = "Azure Search Service";     // Learn more here: https://azure.microsoft.com/en-us/documentation/articles/search-what-is-azure-search/
        static string searchServiceAPIKey = "Azure Search Admin API Key";

        static string indexName = "textanalytics";
        static SearchServiceClient serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(searchServiceAPIKey));
        static ISearchIndexClient indexClient = serviceClient.Indexes.GetClient(indexName);

        private const string KeyField = "metadata_storage_name";
        private const string SummaryField = "summary";
        private const string KeyPhrasesField = "keyPhrases";

        static void Main(string[] args)
        {
            string content = "It has arrived: Windows 10 version 1709, build 16299, the Fall Creators Update. Members of the Windows Insider program have been able to use this latest iteration for a while now, but today's the day it will hit Windows Update for the masses. " + 
                "As with the Creators Update earlier this year, the Windows Update deployment will be slow to start off with.After a spate of issues around the Anniversary Update, which shipped in 2016, Microsoft took a more measured approach with the Creators Update. It took about five months for the previous update to reach two - thirds of machines, as the company rolled the operating system out first to systems known to be compatible, then expanded its reach to an ever larger range of hardware and software, and finally opened the floodgates and offered it to(almost) any Windows 10 machine. " +
                "Again like the Creators Update, anyone who is impatient and wants to forcibly install the new version will be able to do so with the Update Assistant and Media Creation Tool when they get updated, presumably at some point today. " +
                "The Fall Creators Update contains an almost random selection of new features and improvements. Some of the built-in apps have been updated, though many of the apps are now notionally decoupled from the base operating system, because they're distributed and updated through the Store, their releases are somewhat synchronized with operating system updates anyway. Other low-level parts of the operating system are being re-engineered, some new features have been added, and some features have been removed from some SKUs and pushed into more expensive ones. A handful of features do continue the 3D content creation theme started in the Creators Update, but the rest are all over the place. And a couple of things that should have been more of a theme of this update aren't; they're more of a work in progress. " +
                "In spite of this, the Fall Creators Update may yet prove to be a bit special because of the other thing that is becoming available today: Windows Mixed Reality headsets. The Creators Update included early support for the development of virtual and augmented reality applications, but it required the use of developer mode to enable.With version 1709, it's no longer gated—Mixed Reality support is lit up for everyone.";

            // Take the top 20 key phrases
            List<string> KeyPhrases = ExtractKeyPhrases(content).Take(20).ToList();
            // Output the key phrases
            Console.WriteLine("Key Phrases");
            Console.WriteLine("================");
            foreach (var phrase in KeyPhrases)
            {
                Console.WriteLine(phrase);
            }

            // Find the sentences that best represent the content - Summarization
            var sentences = content.Split('!', '.', '?');
            var MatchList = GetBestMatches(sentences, KeyPhrases).Take(SentencesToSummarize).OrderBy(x => x.Sentence).ToList();
            List<string> SentenceList = new List<string>();
            for (int i = 0; i < MatchList.Count; i++)
            {
                SentenceList.Add(sentences[MatchList[i].Sentence].Trim() + ". ");
            }
            // If there are no sentences found, just take the first three
            if (SentenceList.Count == 0)
            {
                for (int i = 0; i < Math.Min(SentencesToSummarize, sentences.Count()); i++)
                {
                    SentenceList.Add(sentences[0].Trim() + ". ");
                }
            }

            // Output the key sentences
            string summary = string.Empty;
            Console.WriteLine("\r\nDocument Summary");
            Console.WriteLine("================");
            foreach (var sentence in SentenceList)
            {
                summary += sentence;
                Console.WriteLine(sentence);
            }

            // Note, this will create a new Azure Search Index for the summary and the key phrases
            Console.WriteLine("\r\nCreating Azure Search index...");
            CreateIndex(serviceClient, indexName);
            UploadDocuments(indexClient, "1", summary, KeyPhrases);

            SearchDocuments(indexClient, "windows");
        }

        static List<Match> GetBestMatches(string[] sentences, List<string> words)
        {
            List<Match> matchList = new List<Match>();
            int counter = 0;
            foreach (var sentence in sentences)
            {
                double count = 0;

                Match match = new Match();
                foreach (var phrase in words)
                {
                    if ((sentence.ToLower().IndexOf(phrase.ToLower()) > -1) &&
                        (sentence.Length > 20) && (WordCount(sentence) >= 3))
                        count += 1;
                }

                if (count > 0)
                    matchList.Add(new Match { Sentence = counter, Total = count });
                counter++;
            }

            return matchList.OrderByDescending(x => x.Total).ToList();
        }

        static int WordCount(string text)
        {
            // Calculate total word count in text
            int wordCount = 0, index = 0;

            while (index < text.Length)
            {
                // check if current char is part of a word
                while (index < text.Length && !char.IsWhiteSpace(text[index]))
                    index++;

                wordCount++;

                // skip whitespace until next word
                while (index < text.Length && char.IsWhiteSpace(text[index]))
                    index++;
            }

            return wordCount;
        }

        static IList<string> ExtractKeyPhrases(string content)
        {
            // Create a client.
            ITextAnalyticsAPI client = new TextAnalyticsAPI();
            client.AzureRegion = AzureRegions.Westus;
            client.SubscriptionKey = TextAnalyticsAPIKey;

            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Getting key-phrases
            KeyPhraseBatchResult result2 = client.KeyPhrases(
                    new MultiLanguageBatchInput(
                        new List<MultiLanguageInput>()
                        {
                          new MultiLanguageInput("en", "1", content)
                        }));


            // Since I am only sending one document, I can return just the first one
            return result2.Documents[0].KeyPhrases;

        }

        public static void CreateIndex(SearchServiceClient serviceClient, string indexName)
        {

            if (serviceClient.Indexes.Exists(indexName))
            {
                serviceClient.Indexes.Delete(indexName);
            }

            var definition = new Index()
            {
                Name = indexName,
                Fields = new[]
                {
                    new Field("fileId", DataType.String)                            { IsKey = true },
                    new Field("summary", DataType.String)                          { IsSearchable = true, IsFilterable = false, IsSortable = false, IsFacetable = false },
                    new Field("keyPhrases", DataType.Collection(DataType.String))   { IsSearchable = true, IsFilterable = true,  IsFacetable = true }
                }
            };

            serviceClient.Indexes.Create(definition);
        }

        public static void UploadDocuments(ISearchIndexClient indexClient, string fileId, string summary, List<string> keyPhrases)
        {
            // This is really inefficient as I should be batching the uploads
            List<IndexAction> indexOperations = new List<IndexAction>();
            var doc = new Document();
            doc.Add("fileId", fileId);
            doc.Add("summary", summary);
            doc.Add("keyPhrases", keyPhrases);
            indexOperations.Add(IndexAction.Upload(doc));

            try
            {
                indexClient.Documents.Index(new IndexBatch(indexOperations));
            }
            catch (IndexBatchException e)
            {
                // Sometimes when your Search service is under load, indexing will fail for some of the documents in
                // the batch. Depending on your application, you can take compensating actions like delaying and
                // retrying. For this simple demo, we just log the failed document keys and continue.
                Console.WriteLine(
                "Failed to index some of the documents: {0}",
                       String.Join(", ", e.IndexingResults.Where(r => !r.Succeeded).Select(r => r.Key)));
            }

        }


        public static void SearchDocuments(ISearchIndexClient indexClient, string searchText)
        {
            // Search using the supplied searchText and output documents that match 
            try
            {
                var sp = new SearchParameters();

                var response = indexClient.Documents.Search(searchText, sp);
                foreach (var result in response.Results)
                {
                    Console.WriteLine("File ID: {0}", result.Document["fileId"]);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed search: {0}", e.Message.ToString());
            }

        }

    }

    public class Match
    {
        public int Sentence { get; set; }
        public double Total { get; set; }
    }

}
