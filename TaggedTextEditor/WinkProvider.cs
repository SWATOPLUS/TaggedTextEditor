using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EdgeJs;

namespace TaggedTextEditor
{
    public class WinkProvider
    {
        private readonly Func<object, Task<object>> _tagSentencesFunc;
        private readonly Func<object, Task<object>> _splitToSentencesFunc;

        public WinkProvider()
        {
            _splitToSentencesFunc = Edge.Func(@"
                var nlp = require('wink-nlp-utils');

                return function (data, callback) {
                    var pureData = data.split('\n').map(x => x.trim()).filter(x => x!=='').join(' ');
                    var sentences = nlp.string.sentences(pureData).join('\n');
                    callback(null, sentences);
                };
            ");
            _tagSentencesFunc = Edge.Func(@"
                var posTagger = require('wink-pos-tagger');
                var tagger = posTagger();

                function tagArrayToText(tags) {
	                return tags.map(x => x.value + '/' + x.pos).join(', ');
                }

                return function (data, callback) {
                    var result = data.map(x => tagger.tagSentence(x)).map(x => tagArrayToText(x)).join('\n');
                    callback(null, result);
                }
            ");

        }

        public string TagText(string text)
        {
            var result = TagSentences(SplitToSentences(text));

            return string.Join("\n", result);
        }

        public string[] TagSentences(IEnumerable<string> sentences)
        {
            return _tagSentencesFunc(sentences.ToArray())
                .GetAwaiter()
                .GetResult()
                .ToString()
                .Split('\n');
        }

        public string[] SplitToSentences(string text)
        {
            return _splitToSentencesFunc(text)
                .GetAwaiter()
                .GetResult()
                .ToString()
                .Split('\n');
        }
    }
}
