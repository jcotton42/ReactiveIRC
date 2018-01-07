using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ReactiveIRC {
    public struct IRCMessage {
        private static readonly Regex MessageRegex =
            new Regex(@"(@(?<tags>[^ ]*) +)?(:(?<prefix>[^ ]*) +)?(?<verb>[^ ]+)( +(?<params>.*))?");

        public ImmutableDictionary<string, string> Tags { get; }
        public string Source { get; }
        public string Verb { get; }
        public ImmutableArray<string> Parameters { get; }

        public IRCMessage(ImmutableDictionary<string, string> tags, string source, string verb,
            ImmutableArray<string> parameters) {
            Tags = tags ?? throw new ArgumentNullException(nameof(tags));
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Verb = verb?.ToUpperInvariant() ?? throw new ArgumentNullException(nameof(verb));
            Parameters = parameters;
        }

        public override string ToString() {
            var sb = new StringBuilder();

            if(!Tags.IsEmpty) {
                sb
                    .Append("@")
                    .Append(string.Join(";", Tags.Select(kvp => $"{kvp.Key}{HandleValue(kvp.Value)}")))
                    .Append(" ");
            }

            if(!Source.Equals(string.Empty)) {
                sb.Append(":").Append(Source).Append(" ");
            }

            sb.Append(Verb);

            if(!Parameters.IsEmpty) {
                if(Parameters.Length == 1) {
                    sb.Append(" :").Append(Parameters[0]);
                } else {
                    sb
                        .Append(" ")
                        .Append(string.Join(" ", Parameters.Take(Parameters.Length - 1)))
                        .Append(" :")
                        .Append(Parameters[Parameters.Length - 1]);
                }
            }

            return sb.ToString();

            string HandleValue(string value) {
                if(value.Equals("")) {
                    return "";
                }

                return new StringBuilder("=" + value)
                    .Replace(";", "\\:")
                    .Replace(" ", "\\s")
                    .Replace("\\", "\\\\")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .ToString();
            }
        }

        public static IRCMessage Parse(string message) {
            if(message is null) {
                throw new ArgumentNullException(nameof(message));
            }

            var matches = MessageRegex.Match(message);
            if(!matches.Success) {
                throw new FormatException($"`{message}` is not a valid IRC message.");
            }

            return new IRCMessage(
                ParseTags(matches.Groups["tags"].Value),
                matches.Groups["prefix"].Value,
                matches.Groups["verb"].Value,
                ParseParams(matches.Groups["params"].Value)
                );

            ImmutableDictionary<string, string> ParseTags(string tags) {
                if(string.IsNullOrWhiteSpace(tags)) {
                    return ImmutableDictionary<string, string>.Empty;
                }

                return tags.Split(';').Select(ExtractKeyValuePair)
                    .ToImmutableDictionary(kvp => kvp.key, kvp => UnescapeValue(kvp.value));

                (string key, string value) ExtractKeyValuePair(string tag) {
                    var parts = tag.Split(new[] { '=' }, 2);
                    return (parts[0], parts.Length > 1 ? parts[1] : "");
                }

                string UnescapeValue(string value) {
                    if(value.Equals("")) {
                        return "";
                    }

                    var sb = new StringBuilder();

                    for(var i = 0; i < value.Length; i++) {
                        if(value[i] == '\\') {
                            i++;

                            if(i >= value.Length) {
                                break;
                            }

                            switch(value[i]) {
                                case ':':
                                    sb.Append(';');
                                    break;
                                case 's':
                                    sb.Append(' ');
                                    break;
                                case '\\':
                                    sb.Append('\\');
                                    break;
                                case 'r':
                                    sb.Append('\r');
                                    break;
                                case 'n':
                                    sb.Append('\n');
                                    break;
                                default:
                                    sb.Append("\\" + value[i]);
                                    break;
                            }
                        } else {
                            sb.Append(value[i]);
                        }
                    }

                    return sb.ToString();
                }
            }

            ImmutableArray<string> ParseParams(string parameters) {
                if(string.IsNullOrWhiteSpace(parameters)) {
                    return ImmutableArray<string>.Empty;
                }

                var parsed = ImmutableArray.CreateBuilder<string>();
                var parts = parameters.Split(new[] { " :" }, 2, StringSplitOptions.None);
                parsed.AddRange(Regex.Split(parts[0], " +").Select(s => s.Trim(' ')));
                if(parts.Length > 1) {
                    parsed.Add(parts[1]);
                }
                return parsed.ToImmutable();
            }
        }
    }
}
