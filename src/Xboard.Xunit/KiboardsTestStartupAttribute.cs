[AttributeUsage(AttributeTargets.Assembly)]
public class XboardTestStartupAttribute(string className) : Attribute
{
    public string ClassName { get; set; } = className;
}
