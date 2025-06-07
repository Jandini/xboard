using System.Security.Cryptography;
using System.Text;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xboard;

internal static class XboardTestExtensions
{     
    public static string ComputeMD5(this string value) => BitConverter.ToString(MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLower();
    public static void WriteMessage(this IMessageSink messageSink, string message) => messageSink.OnMessage(new DiagnosticMessage(message));
    public static void WriteException(this IMessageSink messageSink, Exception exception) => messageSink.OnMessage(new DiagnosticMessage("{0}\n{1}", exception.Message, exception.StackTrace));
}
