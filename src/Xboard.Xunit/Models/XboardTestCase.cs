namespace Xboard.Models;

class XboardTestCase
{
    public Dictionary<string, List<string>> Traits { get; set; }
    public string DisplayName { get; internal set; }
    public string UniqueId { get; internal set; }
    public XboardTestCaseClass Class { get; internal set; }
    public XboardTestCaseCollection Collection { get; internal set; }
    public XboardTestCaseMethod Method { get; set; }
}