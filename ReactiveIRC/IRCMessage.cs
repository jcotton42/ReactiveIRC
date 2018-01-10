using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ReactiveIRC {
    /// <summary>
    /// Represents an IRC message. This type is immutable once constructed.
    /// </summary>
    public struct IRCMessage : IEquatable<IRCMessage> {
        private static readonly Regex MessageRegex =
            new Regex(@"(@(?<tags>[^ ]*) +)?(:(?<prefix>[^ ]*) +)?(?<verb>[^ ]+)( +(?<params>.*))?");

        /// <summary>
        /// The tags associated with this message.
        /// Equal to <see cref="ImmutableDictionary{TKey, TValue}.Empty"/> if the message had no tags.
        /// </summary>
        public ImmutableDictionary<string, string> Tags { get; }

        /// <summary>
        /// The source the message came from.
        /// Equal to <see cref="string.Empty"/> if the message had no source.
        /// </summary>
        public string Source { get; }

        /// <summary>
        /// The verb for this message.
        /// Always a non-empty value and uppercase.
        /// </summary>
        public string Verb { get; }

        /// <summary>
        /// The parameters for this message.
        /// Equal to <see cref="ImmutableArray{T}.Empty"/> if the message had no parameters.
        /// </summary>
        public ImmutableArray<string> Parameters { get; }

        /// <summary>
        /// Constructs a new IRCMessage.
        /// </summary>
        /// <param name="tags">
        /// The tags associated with the message. May not be <c>null</c> or contain <c>null</c> values.
        /// Use <see cref="ImmutableDictionary{TKey, TValue}.Empty"/> or <see cref="string.Empty"/> instead respectively.
        /// </param>
        /// <param name="source">
        /// The source for this message, usually a nick or host.
        /// May not be <c>null</c>, use <see cref="string.Empty"/> instead.
        /// </param>
        /// <param name="verb">
        /// The verb used in the message, such as "PRIVMSG".
        /// May not be <c>null</c> or entirely whitespace.
        /// </param>
        /// <param name="parameters">
        /// The parameters for the message. May not be null or contain <c>null</c> values.
        /// Use <see cref="ImmutableArray{T}.Empty"/> or <see cref="string.Empty"/> instead respectively.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <para>Any of the parameters are <c>null</c>.</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <para><paramref name="verb"/> is empty or contains whitespace.</para>
        /// <para>-or-</para>
        /// <para><paramref name="tags"/> or <paramref name="parameters"/> contain <c>null</c> values.</para>
        /// <para>-or-</para>
        /// <para>Any but the element of <paramref name="parameters"/> contains a space.</para>
        /// </exception>
        /// <remarks><paramref name="source"/> is usually ignored by IRC servers when sent by clients.</remarks>
        public IRCMessage(ImmutableDictionary<string, string> tags, string source, string verb,
            ImmutableArray<string> parameters) {
            Tags = tags ?? throw new ArgumentNullException(nameof(tags));
            Source = source ?? throw new ArgumentNullException(nameof(source));
            Verb = verb?.ToUpperInvariant() ?? throw new ArgumentNullException(nameof(verb));
            Parameters = parameters;

            if(Verb == "" || Regex.IsMatch(verb, "\\s")) {
                throw new ArgumentException(nameof(verb) + " may not be empty or contain whitespace.");
            }
            if(Tags.Keys.Any(k => k == "" || Regex.IsMatch(k, "\\s"))) {
                throw new ArgumentException("Tag keys may not be empty or contain whitespace.");
            }
            if(Tags.Values.Contains(null)) {
                throw new ArgumentException(nameof(tags) + " must not contain null values, use empty string instead.");
            }
            for(var i = 0; i < Parameters.Length; i++) {
                if(Parameters[i] is null) {
                    throw new ArgumentException(nameof(parameters) + " must not contain null values, use empty string instead.");
                }
                if(i != Parameters.Length - 1 && Parameters[i].Contains(" ")) {
                    throw new ArgumentException("Only the last parameter in an IRC message may contain whitespace.");
                }
            }
        }

        /// <summary>
        /// Convenience method to create a new IRC message with the specified verb and parameters.
        /// </summary>
        /// <param name="verb">The verb to use in the message.</param>
        /// <param name="parameters">The parameters to use in the message.</param>
        /// <returns>A new <see cref="IRCMessage"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <para>Any of the parameters are null.</para>
        /// <para>-or-</para>
        /// <para><paramref name="parameters"/> contains null values.</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="verb"/> is null or entirely whitespace.
        /// </exception>
        public static IRCMessage Create(string verb, params string[] parameters) {
            return Create(ImmutableDictionary<string, string>.Empty, verb, parameters);
        }

        /// <summary>
        /// Convenience method to create a new IRC message with the specified tags, verb, and parameters.
        /// </summary>
        /// <param name="tags">The tags to use in the message.</param>
        /// <param name="verb">The verb to use in the message.</param>
        /// <param name="parameters">The parameters to use in the message.</param>
        /// <returns>A new <see cref="IRCMessage"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <para>Any of the parameters are null.</para>
        /// <para>-or-</para>
        /// <para><paramref name="tags"/> or <paramref name="parameters"/> contain null values.</para>
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="verb"/> is null or entirely whitespace.
        /// </exception>
        public static IRCMessage Create(ImmutableDictionary<string, string> tags, string verb, params string[] parameters) {
            return new IRCMessage(
                tags,
                "",
                verb,
                parameters?.ToImmutableArray() ?? throw new ArgumentNullException(nameof(parameters)));
        }

        /// <summary>
        /// Creates a new <see cref="IRCMessageBuilder"/> that can be used to build a new <see cref="IRCMessage"/>.
        /// </summary>
        /// <param name="verb">The verb to use in the message.</param>
        /// <returns>A new <see cref="IRCMessageBuilder"/>.</returns>
        public static IRCMessageBuilder CreateBuilder(string verb) {
            if(verb is null) {
                throw new ArgumentNullException(nameof(verb));
            }
            if(verb == "" || Regex.IsMatch(verb, "\\s")) {
                throw new ArgumentException(nameof(verb) + " may not be empty or contain whitespace.");
            }
            return new IRCMessageBuilder(verb);
        }

        /// <summary>
        /// Parses an IRC messages from a given string.
        /// </summary>
        /// <param name="message">The message to be parsed.</param>
        /// <returns>An instance of <see cref="IRCMessage"/> parsed from <paramref name="message"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="message"/> is null.</exception>
        /// <exception cref="FormatException"><paramref name="message"/> is not a valid IRC message.</exception>
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
                                    sb.Append(value[i]);
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

                if(parameters.StartsWith(":")) {
                    return ImmutableArray.Create(parameters.Substring(1));
                }

                var parsed = ImmutableArray.CreateBuilder<string>();
                var parts = parameters.Split(new[] { " :" }, 2, StringSplitOptions.None);
                parsed.AddRange(parts[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                if(parts.Length > 1) {
                    parsed.Add(parts[1]);
                }
                return parsed.ToImmutable();
            }
        }

        /// <summary>
        /// Compares this <see cref="IRCMessage"/> to another object for equality.
        /// </summary>
        /// <param name="obj">The object to compare to.</param>
        /// <returns><c>true</c> if <paramref name="obj"/> is an <see cref="IRCMessage"/> and equal, <c>false</c> otherwise,</returns>
        public override bool Equals(object obj) => obj is IRCMessage msg && Equals(msg);

        /// <summary>
        /// Compares this IRCMessage to another one for equality.
        /// </summary>
        /// <param name="msg">The message to compare to.</param>
        /// <returns><c>true</c> if the messages are equal in tags, source, verb, and parameters. <c>false</c> otherwise.</returns>
        public bool Equals(IRCMessage msg) => Source.Equals(msg.Source)
                && Verb.Equals(msg.Verb)
                && Tags.Count == msg.Tags.Count
                && Parameters.Length == msg.Parameters.Length
                && !Tags.Except(msg.Tags).Any()
                && Parameters.SequenceEqual(msg.Parameters);

        public static bool operator ==(IRCMessage lhs, IRCMessage rhs) => lhs.Equals(rhs);
        public static bool operator !=(IRCMessage lhs, IRCMessage rhs) => !lhs.Equals(rhs);

        /// <summary>
        /// Returns the string representation of the message in a format suitable for sending to a server.
        /// </summary>
        /// <returns>The string version of this message.</returns>
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
                    .Replace("\\", "\\\\")
                    .Replace(";", "\\:")
                    .Replace(" ", "\\s")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .ToString();
            }
        }
    }
}
