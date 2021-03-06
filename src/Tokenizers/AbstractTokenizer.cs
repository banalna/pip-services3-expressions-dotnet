﻿using System;
using System.Collections.Generic;

using PipServices3.Expressions.IO;
using PipServices3.Expressions.Tokenizers.Utilities;

namespace PipServices3.Expressions.Tokenizers
{
    /// <summary>
    /// Implements an abstract tokenizer class.
    /// </summary>
    public abstract class AbstractTokenizer : ITokenizer
    {
        private CharReferenceMap<ITokenizerState> _map = new CharReferenceMap<ITokenizerState>();

        private bool _skipUnknown;
        private bool _skipWhitespaces;
        private bool _skipComments;
        private bool _skipEof;
        private bool _mergeWhitespaces;
        private bool _unifyNumbers;
        private bool _decodeStrings;

        private ICommentState _commentState;
        private INumberState _numberState;
        private IQuoteState _quoteState;
        private ISymbolState _symbolState;
        private IWhitespaceState _whitespaceState;
        private IWordState _wordState;

        private IPushbackReader _reader;
        private Token _nextToken;
        private TokenType _lastTokenType = TokenType.Unknown;

        protected AbstractTokenizer()
        {
        }

        public bool SkipUnknown
        {
            get { return _skipUnknown; }
            set { _skipUnknown = value; }
        }

        public bool SkipWhitespaces
        {
            get { return _skipWhitespaces; }
            set { _skipWhitespaces = value; }
        }

        public bool SkipComments
        {
            get { return _skipComments; }
            set { _skipComments = value; }
        }

        public bool SkipEof
        {
            get { return _skipEof; }
            set { _skipEof = value; }
        }

        public bool MergeWhitespaces
        {
            get { return _mergeWhitespaces; }
            set { _mergeWhitespaces = value; }
        }

        public bool UnifyNumbers
        {
            get { return _unifyNumbers; }
            set { _unifyNumbers = value; }
        }

        public bool DecodeStrings
        {
            get { return _decodeStrings; }
            set { _decodeStrings = value; }
        }

        public ICommentState CommentState
        {
            get { return _commentState; }
            set { _commentState = value; }
        }

        public INumberState NumberState
        {
            get { return _numberState; }
            set { _numberState = value; }
        }

        public IQuoteState QuoteState
        {
            get { return _quoteState; }
            set { _quoteState = value; }
        }

        public ISymbolState SymbolState
        {
            get { return _symbolState; }
            set { _symbolState = value; }
        }

        public IWhitespaceState WhitespaceState
        {
            get { return _whitespaceState; }
            set { _whitespaceState = value; }
        }

        public IWordState WordState
        {
            get { return _wordState; }
            set { _wordState = value; }
        }

        public ITokenizerState GetCharacterState(char symbol)
        {
            return _map.Lookup(symbol);
        }

        public void SetCharacterState(char fromSymbol, char toSymbol, ITokenizerState state)
        {
            _map.AddInterval(fromSymbol, toSymbol, state);
        }

        public void ClearCharacterStates()
        {
            _map.Clear();
        }

        public IPushbackReader Reader
        {
            get { return _reader; }
            set
            {
                _reader = value;
                _nextToken = null;
                _lastTokenType = TokenType.Unknown;
            }
        }

        public bool HasNextToken()
        {
            _nextToken = _nextToken == null ? ReadNextToken() : _nextToken;
            return _nextToken != null;
        }

        public Token NextToken()
        {
            Token token = _nextToken == null ? ReadNextToken() : _nextToken;
            _nextToken = null;
            return token;
        }

        private Token ReadNextToken()
        {
            if (_reader == null)
            {
                return null;
            }

            Token token = null;

            while (true)
            {
                // Read character
                char nextChar = _reader.Peek();

                // If reached Eof then exit
                if (CharValidator.IsEof(nextChar))
                {
                    token = null;
                    break;
                }

                // Get state for character
                ITokenizerState state = GetCharacterState(nextChar);
                if (state != null)
                {
                    token = state.NextToken(_reader, this);
                }

                // Check for unknown characters and endless loops...
                if (token == null || string.IsNullOrEmpty(token.Value))
                {
                    token = new Token(TokenType.Unknown, _reader.Read().ToString());
                }

                // Skip unknown characters if option set.
                if (token.Type == TokenType.Unknown && _skipUnknown)
                {
                    _lastTokenType = token.Type;
                    continue;
                }

                // Decode strings is option set.
                if (state is IQuoteState && _decodeStrings)
                {
                    token = new Token(token.Type, QuoteState.DecodeString(token.Value, nextChar));
                }

                // Skips comments if option set.
                if (token.Type == TokenType.Comment && _skipComments)
                {
                    _lastTokenType = token.Type;
                    continue;
                }

                // Skips whitespaces if option set.
                if (token.Type == TokenType.Whitespace
                    && _lastTokenType == TokenType.Whitespace
                    && _skipWhitespaces)
                {
                    _lastTokenType = token.Type;
                    continue;
                }

                // Unifies whitespaces if option set.
                if (token.Type == TokenType.Whitespace && _mergeWhitespaces)
                {
                    token = new Token(TokenType.Whitespace, " ");
                }

                // Unifies numbers if option set.
                if (_unifyNumbers
                    && (token.Type == TokenType.Integer
                    || token.Type == TokenType.Float
                    || token.Type == TokenType.HexDecimal))
                {
                    token = new Token(TokenType.Number, token.Value);
                }

                break;
            }

            // Adds an Eof if option is not set.
            if (token == null && _lastTokenType != TokenType.Eof && !_skipEof)
            {
                token = new Token(TokenType.Eof, null);
            }

            // Assigns the last token type
            _lastTokenType = token != null ? token.Type : TokenType.Eof;

            return token;
        }

        public IList<Token> TokenizeStream(IPushbackReader reader)
        {
            Reader = reader;
            IList<Token> tokenList = new List<Token>();
            for (Token token = NextToken(); token != null; token = NextToken())
            {
                tokenList.Add(token);
            }
            return tokenList;
        }

        public IList<Token> TokenizeBuffer(string buffer)
        {
            StringPushbackReader reader = new StringPushbackReader(buffer);
            return TokenizeStream(reader);
        }

        public IList<string> TokenizeStreamToStrings(IPushbackReader reader)
        {
            Reader = reader;
            IList<string> stringList = new List<string>();
            for (Token token = NextToken(); token != null; token = NextToken())
            {
                stringList.Add(token.Value);
            }
            return stringList;
        }

        public IList<string> TokenizeBufferToStrings(string buffer)
        {
            StringPushbackReader reader = new StringPushbackReader(buffer);
            return TokenizeStreamToStrings(reader);
        }
    }
}
