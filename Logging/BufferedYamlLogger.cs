using System;
using System.IO;
using System.Reflection;

namespace Silksong.GameObjectDump.Logging;

public class BufferedYamlLogger
{
    private static string _assemblyParentFolder = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;
    
    private readonly string _filePath;

    public BufferedYamlLogger(string fileName)
    {
        _filePath = Path.Combine(_assemblyParentFolder, fileName);
        WriteTimestampHeader();
    }

    /// <summary>
    /// Writes a timestamp comment line to the log file when the logger is created.
    /// Example: "# Log created at: 2025-10-23 15:42:10"
    /// </summary>
    private void WriteTimestampHeader()
    {
        using var writer = File.AppendText(_filePath);
        writer.WriteLine($"# Log created at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    }

    /// <summary>
    /// Logs a top-level NestedLog object to the file in YAML format.
    /// Each top-level object is buffered in memory and flushed once.
    /// A YAML document separator '---' is written before each log.
    /// </summary>
    public void Log(NestedLog log)
    {
        FlushToFile(log.ToYaml());
    }

    /// <summary>
    /// Writes the YAML string to the file, prepending a separator.
    /// </summary>
    private void FlushToFile(string yamlText)
    {
        using var writer = File.AppendText(_filePath);
        writer.WriteLine("---");        // YAML document separator
        writer.WriteLine(yamlText);
    }
}
