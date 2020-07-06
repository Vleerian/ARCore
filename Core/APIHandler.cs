using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using ARCore.Types;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

using ARCore.Helpers;
using System.Collections.Concurrent;

namespace ARCore.Core
{
    /// <summary>
    /// NSAPIRequest is the 'queue' object for the APIHandler
    /// NSAPIRequest.Run should only be called from NSAPIhandler
    /// </summary>
    public class NSAPIRequest
    {
        public readonly string Uri;
        public bool Done;
        string Result;

        public NSAPIRequest(string uri)
        {
            Uri = uri;
            Done = false;
        }

        public void Run(ref WebClient webClient)
        {
            if (Done)
                return;
            Result = webClient.DownloadString(Uri);
            Done = true;
        }

        public async Task<T> GetResultAsync<T>() where T : NSApi
        {
            while (!Done) await Task.Delay(100); //Wait for it to be done
            return await Extensions.DeserializeObjectAsync<T>(Result);
        }

        public T GetResult<T>() where T : NSApi
        {
            while (!Done) Thread.Sleep(80); //Wait for it to be done
            return Extensions.DeserializeObject<T>(Result);
        }
    
    }

    /// <summary>
    /// APIHandler does what it says it does, handle the NS API
    /// All API requests go through it. It does not contain any
    /// logic of it's own.
    /// </summary>
    class APIHandler
    {
        public string User;
        private SemaphoreSlim webclientMutex;
        private WebClient webClient;
        private ConcurrentQueue<NSAPIRequest> apiQueue;
        private bool Running;
        private FileStream Lock;

        private readonly IServiceProvider _services;

        public APIHandler(IServiceProvider services)
        {
            _services = services;

            Logger.Log(LogEventType.Verbose, "Initializing APIHandler");

            webClient = new WebClient();
            webclientMutex = new SemaphoreSlim(1);
            apiQueue = new ConcurrentQueue<NSAPIRequest>();
        }

        /// <summary>
        /// Unzips nation.xml.gz and region.xml.gz files
        /// </summary>
        /// <param name="Filename">the .xml.gz file to unzip</param>
        /// <returns></returns>
        private static string UnzipDump(string Filename)
        {
            //This will contain the raw XML
            string text;
            //These usings initialize the filestream of the downloaded naiton data dump, and the
            //GZip stream that will decompress it. They'll both be disposed afterwads
            using (var FS = new FileStream(Filename, FileMode.Open))
            using (var GZip = new GZipStream(FS, CompressionMode.Decompress))
            {
                //Initialize the buffer
                const int size = 4096;
                byte[] buffer = new byte[size];
                //Initialize the memory stream that will contain the decompressed object
                using (MemoryStream memory = new MemoryStream())
                {
                    //Read bytes into the buffer until there is nothing left to read.
                    int count = 0;
                    do
                    {
                        count = GZip.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    //convert the decompressed bytes into a string.
                    byte[] decompressed = memory.ToArray();
                    text = Encoding.ASCII.GetString(decompressed);
                }
            }

            return text;
        }

        /// <summary>
        /// Parses the nations.xml an regions.xml files
        /// </summary>
        /// <typeparam name="T">the type of dump you're processing</typeparam>
        /// <param name="Filename">The xml file to parse</param>
        /// <returns></returns>
        public static T ParseDataDump<T>(string Filename) where T : DataDump
        {
            string RawXML;

            //If it's XML, don't bother unzipping it
            if (Filename.EndsWith(".xml"))
                RawXML = File.ReadAllText(Filename); //
            else if (Filename.EndsWith(".xml.gz"))
            {
                //Unzip the XML
                RawXML = UnzipDump(Filename);
            }
            else
                throw new ArgumentException("Invalid data-dump file. Must be an XML file, or GZipped XML file.");

            if(typeof(T) == typeof(CardsDataDump))
            {
                // NS did not use CDATA tags for mottos, which can contain XML special characters.
                // Because of this nastiness, some terribleness is required.
                RawXML = Regex.Replace(RawXML, @"<MOTTO>(.*)</MOTTO>", "<MOTTO><![CDATA[$1]]></MOTTO>");
            }

            //Deserialize the XML into the DataDump object
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using var fs = new StringReader(RawXML);
            T dump = (T)serializer.Deserialize(fs);

            return dump;
        }

        /// <summary>
        /// Donwloads, parses, and returns the latest data dump
        /// </summary>
        /// <typeparam name="T">The type of data dump to download</typeparam>
        /// <returns>A Data dump object</returns>
        public async Task<T> DownloadDataDump<T>() where T : DataDump
        {
            string DumpType = typeof(T) == typeof(RegionDataDump) ? "regions" : "nations";

            if (File.Exists($"{DumpType}.xml.gz"))
            {
                Logger.Log(LogEventType.Warning, $"Old {DumpType} datadump exists - deleting.");
                File.Delete($"{DumpType}.xml.gz");
            }
            await webclientMutex.WaitAsync();
            webClient.DownloadFile($"https://www.nationstates.net/pages/{DumpType}.xml.gz", $"{DateTime.Now.ToString("MM-dd-yy")}-{DumpType}.xml.gz");
            webclientMutex.Release();

            return ParseDataDump<T>($"{DateTime.Now.ToString("MM-dd-yy")}-{DumpType}.xml.gz");
        }

        /// <summary>
        /// Enqueues an NSAPI Request.
        /// </summary>
        /// <param name="request"></param>
        public void Enqueue(out NSAPIRequest request, string Endpoint)
        {
            request = new NSAPIRequest(Endpoint);
            apiQueue.Enqueue(request);
        }

        /// <summary>
        /// Signals the APILoop to shut down
        /// </summary>
        public Task Shutdown(object sender)
        {
            Running = false;
            return Task.CompletedTask;
        }

        /// <summary>
        /// The APILoop that executes NSAPIRequests
        /// </summary>
        /// <returns></returns>
        public async Task APILoop()
        {
            //By putting an exclusive claim on a file, this prevents this instance from being run twice
            Lock = new FileStream(".running", FileMode.Open, FileAccess.Read, FileShare.None);
            Running = true;

            while(Running)
            {
                //This keeps us under the rate limit
                await Task.Delay(800);
                if (!Running) break;
                if(apiQueue.Count > 0)
                {
                    //Dequeue and execute an API Request
                    if(apiQueue.TryDequeue(out NSAPIRequest request))
                    {
                        await webclientMutex.WaitAsync();
                        webClient.Headers.Add("user-agent", $"ARCore - doomjaw@hotmail.com | Current User : {User}");
                        request.Run(ref webClient);
                        webclientMutex.Release();
                    }
                }
            }
            Lock.Close();
            File.Delete(".running");
        }
    }
}
