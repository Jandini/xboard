namespace Xboard.Models;

class XboardTestRun
{
    public string RunId { get; internal set; }
    public string Name { get; internal set; }
    public string Hash { get; internal set; }
    public DateTime StartTime { get; internal set; }
    public string MachineName { get; internal set; }
    public string UserName { get; internal set; }
    public string Status { get; internal set; }
    public Dictionary<string, string> Variables { get; internal set; }
    public string FrameworkVersion { get; internal set; }
    public XboardTestRunSummary Summary { get; set; }   
    public XboardTestCaseRun Test { get; internal set; }
}
