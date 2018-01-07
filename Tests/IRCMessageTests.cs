using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using ReactiveIRC;
using Xunit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Tests {
    public class IRCMessageTests {
        private static MessageTest[] MessageSplitData;

        static IRCMessageTests() {
            var input = new StreamReader("irc-parser-tests/tests/msg-split.yaml");
            var deserializer = new DeserializerBuilder().WithNamingConvention(new CamelCaseNamingConvention()).Build();
            MessageSplitData = deserializer.Deserialize<Root>(input).Tests;
        }

        [Theory]
        [MemberData(nameof(GetSplitData))]
        public void TestMessageParsing(MessageTest data) {
            var message = IRCMessage.Parse(data.Input);
            var atoms = data.Atoms;
            Assert.Equal(atoms.Source ?? "", message.Source);
            Assert.Equal(atoms.Verb.ToUpperInvariant(), message.Verb);
            Assert.Equal(atoms.Params ?? (IEnumerable<string>)ImmutableArray<string>.Empty,
                message.Parameters);
            Assert.Equal(atoms.Tags, message.Tags, new NullFriendlyDictionaryComparer());
        }

        public static IEnumerable<object[]> GetSplitData() {
            foreach(var d in MessageSplitData) {
                yield return new object[] { d };
            }
        }
    }

    public class NullFriendlyDictionaryComparer : IEqualityComparer<IDictionary<string, string>> {
        static InnerNullFriendlyDictionaryComparer comparer = new InnerNullFriendlyDictionaryComparer();

        public bool Equals(IDictionary<string, string> x, IDictionary<string, string> y) {
            return !(x ?? ImmutableDictionary<string, string>.Empty).Except(y, comparer).Any();
        }

        public int GetHashCode(IDictionary<string, string> obj) => obj.GetHashCode();

        class InnerNullFriendlyDictionaryComparer : IEqualityComparer<KeyValuePair<string, string>> {
            public bool Equals(KeyValuePair<string, string> x, KeyValuePair<string, string> y) {
                return x.Key.Equals(y.Key) && (x.Value ?? "").Equals(y.Value ?? "");
            }

            public int GetHashCode(KeyValuePair<string, string> obj) => obj.GetHashCode();
        }
    }

    public class Root {
        public MessageTest[] Tests { get; set; }
    }

    public class MessageTest {
        public string Input { get; set; }
        public MessageAtoms Atoms { get; set; }
    }

    public class MessageAtoms {
        public string Source { get; set; }
        public string Verb { get; set; }
        public string[] Params { get; set; }
        public Dictionary<string, string> Tags { get; set; }
    }
}
