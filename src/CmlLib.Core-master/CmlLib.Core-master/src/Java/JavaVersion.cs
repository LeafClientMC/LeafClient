using System.Text.Json.Serialization;
using CmlLib.Core.Internals;

namespace CmlLib.Core.Java;

[JsonConverter(typeof(JavaVersionConverter))]
public record JavaVersion
{
    public JavaVersion(string component) : this(component, null)
    {
    }

    public JavaVersion(string component, string? majorVersion)
    {
        this.Component = component;
        this.MajorVersion = majorVersion;
    }

    public string Component { get; }
    public string? MajorVersion { get; }
}