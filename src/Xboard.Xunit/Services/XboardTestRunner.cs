using Xboard.Models;
using Nest;
using System.Collections;
using System.Reflection;
using System.Text;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xboard.Services;
internal class XboardTestRunner
{
    private const string XB_RUN_ID = "XB_RUN_ID";
    private readonly XboardElasticClient _elasticService;
    private readonly Func<XboardTestRun> _testRunFactory;
    private readonly string _runId;
    private string _runName;
    private string _runHash;

    public string Version { get; private set; }

    public XboardTestRunner(IMessageSink messageSink)
    {
        _runId = Environment.GetEnvironmentVariable(XB_RUN_ID) is { Length: > 0 } runId
            ? runId
            : Guid.NewGuid().ToString();

        Version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

        var variables = new Dictionary<string, string>();
        var startTime = DateTime.Now;
        var userName = Environment.UserName;
        var machineName = Environment.MachineName;


        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            var name = entry.Key.ToString();
            var value = entry.Value.ToString();
           
            const string prefix = "XB_VAR_";

            if (name.Length > prefix.Length + 1 && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(value))
                variables.TryAdd(name[prefix.Length..], value);

            const string base64 = "XB_B64_";

            if (name.Length > base64.Length + 1 && name.StartsWith(base64, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(value))
            {
                try
                {
                    value = Encoding.UTF8.GetString(Convert.FromBase64String(value));
                }
                catch (Exception ex) 
                {
                    messageSink.WriteException(ex);
                }

                variables.TryAdd(name[base64.Length..], value);
            }
        }

        _testRunFactory = () => new XboardTestRun()
        {
            RunId = _runId,
            StartTime = startTime,
            MachineName = machineName,
            UserName = userName,
            FrameworkVersion = Version,
            Variables = variables,
            Name = _runName,
            Hash = _runHash,
        };

        var startupAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetCustomAttribute<XboardStartupAttribute>() != null).ToArray();

        foreach (var assembly in startupAssemblies)
            Startup(assembly, messageSink);

        var uriString = Environment.GetEnvironmentVariable("XB_ELASTICSEARCH_HOST") ?? "http://localhost:9200";
        var connectionSettings = new ConnectionSettings(new Uri(uriString));

        var elasticClient = new ElasticClient(connectionSettings
            .DefaultMappingFor<XboardTestRun>(m => m
                .IndexName($"xobard-testruns-{DateTime.UtcNow:yyyy-MM}"))
            .MaxRetryTimeout(TimeSpan.FromMinutes(5))
            .EnableApiVersioningHeader()
            .MaximumRetries(3));

        _elasticService = new XboardElasticClient(elasticClient, messageSink);

        messageSink.WriteMessage($"Xboard.Xunit {Version} logging to {uriString}");
    }


    private void Startup(Assembly assembly, IMessageSink messageSink)
    {
        try
        {
            var startup = assembly.GetCustomAttribute<XboardStartupAttribute>();
            Type type = assembly.GetType(startup.ClassName);

            if (type != null)
            {
                messageSink.WriteMessage($"Invoking {type.FullName}");

                if (type.GetConstructor([typeof(string), typeof(IMessageSink)]) != null)
                    Activator.CreateInstance(type, _runId, messageSink);
                else if (type.GetConstructor([typeof(string)]) != null)
                    Activator.CreateInstance(type, _runId);
                else if (type.GetConstructor([typeof(IMessageSink)]) != null)
                    Activator.CreateInstance(type, messageSink);
                else if (type.GetConstructor([]) != null)
                    Activator.CreateInstance(type);
            }
        }
        catch (Exception ex)
        {
            messageSink.WriteException(ex);
        }
    }

    public async Task IndexTestRunAsync(RunSummary summary)
    {
        var testRun = _testRunFactory();

        testRun.Summary = new XboardTestRunSummary()
        {
            Total = summary.Total,
            Failed = summary.Failed,
            Skipped = summary.Skipped,
            Time = summary.Time,
        };

        testRun.Status = summary.Failed > 0 ? "Failed" : summary.Skipped == summary.Total ? "Skipped" : "Passed";

      
        await _elasticService.IndexDocumentAsync(testRun);
    }

    public async Task IndexTestCaseRunAsync(DateTime startedAt, ITestResultMessage testResult)
    {
        var testRun = _testRunFactory();

        testRun.Test = new XboardTestCaseRun()
        {
            Id = (testRun.RunId + testResult.TestCase.UniqueID).ComputeMD5(),
            StartedAt = startedAt,
            FinishedAt = DateTime.UtcNow,
            ExecutionTime = testResult.ExecutionTime,
            Output = testResult.Output,
            Failed = testResult is ITestFailed failed ? new XboardTestCaseRunFailed()
            {
                StackTraces = failed.StackTraces,
                ExceptionTypes = failed.ExceptionTypes,
                Messages = failed.Messages,
            } : null,
            TestCase = new XboardTestCase()
            {
                DisplayName = testResult.TestCase.DisplayName,
                UniqueId = testResult.TestCase.UniqueID,
                Traits = testResult.TestCase.Traits,
                Method = new XboardTestCaseMethod()
                {
                    Name = testResult.TestMethod.Method.Name,
                },
                Class = new XboardTestCaseClass()
                {
                    Name = testResult.TestClass.Class.Name,
                    Assembly = new XboardTestCaseAssembly()
                    {
                        Name = testResult.TestClass.Class.Assembly.Name,
                        AssemblyPath = testResult.TestClass.Class.Assembly.AssemblyPath
                    }
                },
                Collection = new XboardTestCaseCollection()
                {
                    DisplayName = testResult.TestClass.TestCollection.DisplayName,
                    UniqueId = testResult.TestClass.TestCollection.UniqueID,
                }
            },
            Skipped = testResult is ITestSkipped skipped ? new XboardTestCaseRunSkipped() { Reason = skipped.Reason } : null,
            Status = testResult is ITestPassed ? "Passed" : testResult is ITestFailed ? "Failed" : testResult is ITestSkipped ? "Skipped" : "Other"
        };

        await _elasticService.IndexDocumentAsync(testRun);
    }

    internal void UpdateRun(IEnumerable<IXunitTestCase> testCases)
    {
        _runName = string.Join(",", testCases.Select(a => Path.GetFileNameWithoutExtension(a.TestMethod.TestClass.Class.Assembly.AssemblyPath)).Distinct());
        _runHash = string.Join(",", testCases.OrderBy(a => a.UniqueID).Select(a => a.UniqueID)).ComputeMD5();
    }
}
