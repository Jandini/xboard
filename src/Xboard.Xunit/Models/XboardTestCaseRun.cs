namespace Xboard.Models;

class XboardTestCaseRun
{
    public string Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
    public XboardTestCase TestCase { get; set; }
    public XboardTestCaseRunSkipped Skipped { get; set; }
    public XboardTestCaseRunFailed Failed { get; set; }
    public decimal ExecutionTime { get; set; }
    public string Status { get; set; }
    public string Output { get; set; }    
}
