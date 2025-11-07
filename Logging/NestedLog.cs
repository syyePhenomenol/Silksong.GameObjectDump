using System;
using System.Collections.Generic;
using System.Text;

namespace Silksong.GameObjectDump.Logging
{
    public class NestedLog
    {
        private readonly Dictionary<string, object> _entries = [];

        /// <summary>
        /// Read-only access to the log's contents.
        /// Keys are strings; values are string or NestedLog.
        /// </summary>
        public IReadOnlyDictionary<string, object> Entries => _entries;

        /// <summary>
        /// Adds an entry to the log.
        /// - Ignores null, empty strings, and empty NestedLogs.
        /// - Converts all other values to strings using ToString().
        /// </summary>
        public bool Add(string key, object? value)
        {
            switch (value)
            {
                case null:
                    return false; // ignore nulls

                case string s when string.IsNullOrEmpty(s):
                    return false; // ignore empty strings

                case NestedLog nested when nested.Entries.Count == 0:
                    return false; // ignore empty NestedLogs

                case NestedLog:
                    _entries[key] = value;
                    return true;

                case Type t:
                    _entries[key] = t.Name; // store short type name
                    return true;

                default:
                    string stringValue = value.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(stringValue))
                    {
                        _entries[key] = stringValue;
                        return true;
                    }

                return false;
            }
        }

        // Produce indented multiline text
        public override string ToString()
        {
            if (_entries.Count == 0)
                return "<empty log>";

            var sb = new StringBuilder();
            BuildString(sb, 0);
            return sb.ToString();
        }

        private void BuildString(StringBuilder sb, int indent)
        {
            string indentStr = new string(' ', indent * 4);

            foreach (var kvp in _entries)
            {
                switch (kvp.Value)
                {
                    case string value:
                        sb.AppendLine($"{indentStr}{kvp.Key}: {value}");
                        break;
                    case NestedLog nested:
                        sb.AppendLine($"{indentStr}{kvp.Key}:");
                        nested.BuildString(sb, indent + 1);
                        break;
                }
            }
        }

        // Export to YAML string
        public string ToYaml()
        {
            if (_entries.Count == 0)
                return "# (empty)";

            var sb = new StringBuilder();
            BuildYaml(sb, 0);
            return sb.ToString();
        }

        private void BuildYaml(StringBuilder sb, int indent)
        {
            string indentStr = new string(' ', indent * 2); // 2 spaces per YAML level

            foreach (var kvp in _entries)
            {
                switch (kvp.Value)
                {
                    case string value:
                        sb.AppendLine($"{indentStr}{kvp.Key}: {EscapeYaml(value)}");
                        break;
                    case NestedLog nested:
                        sb.AppendLine($"{indentStr}{kvp.Key}:");
                        nested.BuildYaml(sb, indent + 1);
                        break;
                }
            }
        }

        // Minimal YAML escaping for special characters
        private string EscapeYaml(string value)
        {
            if (value.Contains(":") || value.Contains("\"") || value.Contains("'") || value.Contains("\n"))
                return $"\"{value.Replace("\"", "\\\"")}\"";

            return value;
        }
    }
}
