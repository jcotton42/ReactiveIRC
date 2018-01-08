using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace ReactiveIRC {
    /// <summary>
    /// Allows building an <see cref="IRCMessage"/> with the builder pattern.
    /// </summary>
    public sealed class IRCMessageBuilder {
        private readonly ImmutableDictionary<string, string>.Builder tagsBuilder;
        private string source;
        private string verb;
        private readonly ImmutableArray<string>.Builder parametersBuilder;

        internal IRCMessageBuilder(string verb) {
            tagsBuilder = ImmutableDictionary.CreateBuilder<string, string>();
            source = "";
            this.verb = verb;
            parametersBuilder = ImmutableArray.CreateBuilder<string>();
        }

        /// <summary>
        /// Adds a new tag to the message.
        /// </summary>
        /// <param name="key">The key for the tag.</param>
        /// <param name="value">The value for the tag.</param>
        /// <returns>The same <see cref="IRCMessageBuilder"/> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> are <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="key"/> was empty or contained whitespace.</exception>
        public IRCMessageBuilder AddTag(string key, string value) {
            if(key is null) {
                throw new ArgumentNullException(nameof(key));
            }
            if(value is null) {
                throw new ArgumentNullException(nameof(value));
            }
            if(key == "" || Regex.IsMatch(key, "\\s")) {
                throw new ArgumentException("Tag keys may not be empty or contain whitespace.");
            }

            tagsBuilder.Add(key, value);
            return this;
        }

        /// <summary>
        /// Adds a set of tags to the message.
        /// </summary>
        /// <param name="tags">The tags to add.</param>
        /// <returns>The same <see cref="IRCMessageBuilder"/> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="tags"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">
        /// <para>One or more keys in <paramref name="tags"/> are <c>null</c>, empty, or contain whitespace.</para>
        /// <para>-or-</para>
        /// <para>One of the values in <paramref name="tags"/> is <c>null</c>.</para>
        /// </exception>
        public IRCMessageBuilder AddTags(IEnumerable<KeyValuePair<string, string>> tags) {
            if(tags is null) {
                throw new ArgumentNullException(nameof(tags));
            }
            foreach(var kvp in tags) {
                if(kvp.Key is null || kvp.Key == "" || Regex.IsMatch(kvp.Key, "\\s")) {
                    throw new ArgumentException("Tag keys may not be null, empty, or contain whitespace.");
                }
                if(kvp.Value is null) {
                    throw new ArgumentException("Tag values may not be null.");
                }
            }

            tagsBuilder.AddRange(tags);
            return this;
        }

        /// <summary>
        /// Sets the source for the message.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>The same <see cref="IRCMessageBuilder"/> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <c>null</c>.</exception>
        public IRCMessageBuilder SetSource(string source) {
            this.source = source ?? throw new ArgumentNullException(nameof(source));
            return this;
        }

        /// <summary>
        /// Adds a parameter to the message.
        /// </summary>
        /// <param name="parameter">The parameter to add.</param>
        /// <returns>The same <see cref="IRCMessageBuilder"/> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="parameter"/> is <c>null</c>.</exception>
        public IRCMessageBuilder AddParameter(string parameter) {
            parametersBuilder.Add(parameter ?? throw new ArgumentNullException(nameof(parameter)));
            return this;
        }

        /// <summary>
        /// Adds multiple parameters to the message.
        /// </summary>
        /// <param name="parameters">The parameters to add.</param>
        /// <returns>The same <see cref="IRCMessageBuilder"/> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="parameters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="parameters"/> contained one or more <c>null</c> entries.</exception>
        public IRCMessageBuilder AddParameters(IEnumerable<string> parameters) {
            if(parameters is null) {
                throw new ArgumentNullException(nameof(parameters));
            }
            if(parameters.Contains(null)) {
                throw new ArgumentException("Parameters may not be null.");
            }
            parametersBuilder.AddRange(parameters);
            return this;
        }

        /// <summary>
        /// Adds multiple parameters to the message.
        /// </summary>
        /// <param name="parameters">The parameters to add.</param>
        /// <returns>The same <see cref="IRCMessageBuilder"/> instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="parameters"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="parameters"/> contained one or more <c>null</c> entries.</exception>
        public IRCMessageBuilder AddParameters(params string[] parameters) {
            return AddParameters((IEnumerable<string>)parameters);
        }

        /// <summary>
        /// Builds a new <see cref="IRCMessage"/>.
        /// </summary>
        /// <returns>A new <see cref="IRCMessage"/> instance.</returns>
        public IRCMessage Build() => new IRCMessage(
                tagsBuilder.ToImmutable(),
                source,
                verb,
                parametersBuilder.ToImmutable()
                );
    }
}
