// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
//
// Azure Search: http://azure.com
// Project Oxford: http://ProjectOxford.ai
//
// Copyright (c) Microsoft Corporation
// All rights reserved.
//
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using Microsoft.Azure.Search;
using System;
using System.Configuration;
using System.IO;
using System.Threading;

namespace AzureSearchTextAnalytics
{
    class Program
    {
        static string searchServiceName = [azure search service];     // Learn more here: https://azure.microsoft.com/en-us/documentation/articles/search-what-is-azure-search/
        static string searchServiceAPIKey = [azure search service api key];
        static string azureMLTextAnalyticsKey = [azure ml text analytics key];     // Learn more here: https://azure.microsoft.com/en-us/documentation/articles/machine-learning-apps-text-analytics/

        static string indexName = "textanalytics";
        static SearchServiceClient serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(searchServiceAPIKey));
        static SearchIndexClient indexClient = serviceClient.Indexes.GetClient(indexName);

        static void Main(string[] args)
        {
            string filetext = "Build great search experiences for your web and mobile apps. " +
                "Many applications use search as the primary interaction pattern for their users. When it comes to search, user expectations are high. They expect great relevance, suggestions, near-instantaneous responses, multiple languages, faceting, and more. Azure Search makes it easy to add powerful and sophisticated search capabilities to your website or application. The integrated Microsoft natural language stack, also used in Bing and Office, has been improved over 16 years of development. Quickly and easily tune search results, and construct rich, fine-tuned ranking models to tie search results to business goals. Reliable throughput and storage provide fast search indexing and querying to support time-sensitive search scenarios. " +
                "Reduce complexity with a fully managed service. " +
                "Azure Search removes the complexity of setting up and managing your own search index. This fully managed service helps you avoid the hassle of dealing with index corruption, service availability, scaling, and service updates. Create multiple indexes with no incremental cost per index. Easily scale up or down as the traffic and data volume of your application changes.";

            // Note, this will create a new Azure Search Index for the text and the key phrases
            Console.WriteLine("Creating Azure Search index...");
            AzureSearch.CreateIndex(serviceClient, indexName);

            // Apply the Machine Learning Text Extraction to retrieve only the key phrases
            Console.WriteLine("Extracting key phrases from processed text... \r\n");
            KeyPhraseResult keyPhraseResult = TextExtraction.ProcessText(azureMLTextAnalyticsKey, filetext);

            Console.WriteLine("Found the following phrases... \r\n");
            foreach (var phrase in keyPhraseResult.KeyPhrases)
                Console.WriteLine(phrase);

            // Take the resulting key phrases to a new Azure Search Index
            // It is highly recommended that you upload documents in batches rather 
            // individually like is done here
            Console.WriteLine("Uploading extracted text to Azure Search...\r\n");
            AzureSearch.UploadDocuments(indexClient, "1", keyPhraseResult);
            Console.WriteLine("Wait 5 seconds for content to become searchable...\r\n");
            Thread.Sleep(5000);

            // Execute a test search 
            Console.WriteLine("Execute Search...");
            AzureSearch.SearchDocuments(indexClient, "Azure Search");

            Console.WriteLine("All done.  Press any key to continue.");
            Console.ReadLine();

        }
    }
}
