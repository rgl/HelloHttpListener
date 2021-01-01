using System;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

namespace HelloHttpListener
{
    class Program
    {
        const int ERROR_ACCESS_DENIED = 5;
        const int ERROR_OPERATION_ABORTED = 995;

        static void Main(string[] args)
        {
            var prefixes = args;

            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("Windows XP SP2 or Server 2003 is required to use the HttpListener class.");
                return;
            }

            if (prefixes == null || prefixes.Length == 0)
            {
                Console.WriteLine("Please specify prefixes in the command line arguments.");
                return;
            }

            var prefixErrors = prefixes.Select(
                p =>
                    {
                        if (!Regex.IsMatch(p, @"^https?://(([a-z_-]+[a-z0-9_-]+\.?)+|[*+]):\d+/"))
                        {
                            return string.Format("Invalid URI {0} (hint: must have an explicit port and end with a trailing forward slash)", p);
                        }
                        return null;
                    }
                ).Where(p => p != null).ToArray();
            foreach (var e in prefixErrors)
            {
                Console.WriteLine("ERROR: {0}", e);
            }
            if (prefixErrors.Length > 0)
            {
                return;
            }

            var listener = new HttpListener();

            foreach (string s in prefixes)
            {
                Console.WriteLine("Adding listener prefix {0}...", s);
                listener.Prefixes.Add(s);
            }

            try
            {
                listener.Start();
            }
            catch (HttpListenerException e)
            {
                if (e.ErrorCode == ERROR_ACCESS_DENIED)
                {
                    Console.WriteLine("Access was denied.");
                    Console.WriteLine();
                    Console.WriteLine("Ensure the URL ACLs by executing the following commmands in a administrative powershell session:");
                    Console.WriteLine();
                    foreach (var s in prefixes)
                    {
                        Console.WriteLine(
                            "netsh http add urlacl \"url={0}\" \"user={1}\" listen=yes",
                            s,
                            WindowsIdentity.GetCurrent().Name);
                    }
                }
                throw;
            }

            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) =>
            {
                e.Cancel = true;
                listener.Stop();
            };

            Console.WriteLine("Press Ctrl+C to quit");
            Console.WriteLine("Listening...");

            try
            {
                while (true)
                {
                    var context = listener.GetContext();
                    var request = context.Request;

                    if (request.Url.PathAndQuery.EndsWith("/quit"))
                    {
                        SendResponse(context.Response, "Bye bye");
                        break;
                    }
                    else
                    {
                        var message = string.Format("HTTP {0} {1}", request.HttpMethod, request.Url);
                        Console.WriteLine(message);
                        SendResponse(context.Response, message);
                    }
                }
            }
            catch (HttpListenerException e)
            {
                if (e.ErrorCode != ERROR_OPERATION_ABORTED)
                {
                    throw;
                }
            }

            listener.Stop();

            Console.WriteLine("Bye bye");
        }

        private static void SendResponse(HttpListenerResponse response, string text)
        {
            var buffer = Encoding.UTF8.GetBytes(text);

            response.ContentType = "text/plain";
            response.ContentLength64 = buffer.Length;

            var output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            output.Close();
        }
    }
}
