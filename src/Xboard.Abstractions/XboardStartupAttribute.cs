[AttributeUsage(AttributeTargets.Assembly)]
public class XboardStartupAttribute(string className) : Attribute
{
    public string ClassName { get; set; } = className;
}
