﻿using System;
using System.Text;

using PipServices3.Expressions.IO;
using PipServices3.Expressions.Tokenizers.Utilities;

namespace PipServices3.Expressions.Tokenizers.Generic
{
    /// <summary>
    /// A quoteState returns a quoted string token from a reader. This state will collect characters
    /// until it sees a match to the character that the tokenizer used to switch to this state.
    /// For example, if a tokenizer uses a double-quote character to enter this state,
    /// then <code>nextToken()</code> will search for another double-quote until it finds one
    /// or finds the end of the reader.
    /// </summary>
    public class GenericQuoteState : IQuoteState
    {
        /// <summary>
        /// Return a quoted string token from a reader. This method will collect
        /// characters until it sees a match to the character that the tokenizer used
        /// to switch to this state.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="tokenizer"></param>
        /// <returns>A quoted string token from a reader.</returns>
        public virtual Token NextToken(IPushbackReader reader, ITokenizer tokenizer)
        {
            char firstSymbol = reader.Read();
            StringBuilder tokenValue = new StringBuilder();
            tokenValue.Append(firstSymbol);

            for (char nextSymbol = reader.Read(); !CharValidator.IsEof(nextSymbol); nextSymbol = reader.Read())
            {
                tokenValue.Append(nextSymbol);
                if (nextSymbol == firstSymbol)
                {
                    break;
                }
            }

            return new Token(TokenType.Quoted, tokenValue.ToString());
        }

        /// <summary>
        /// Encodes a string value.
        /// </summary>
        /// <param name="value">A string value to be encoded.</param>
        /// <param name="quoteSymbol">A string quote character.</param>
        /// <returns>An encoded string.</returns>
        public virtual string EncodeString(string value, char quoteSymbol)
        {
            if (value == null) return null;

            StringBuilder result = new StringBuilder();
            result.Append(quoteSymbol).Append(value).Append(quoteSymbol);
            return result.ToString();
        }

        /// <summary>
        /// Decodes a string value.
        /// </summary>
        /// <param name="value">A string value to be decoded.</param>
        /// <param name="quoteSymbol">A string quote character.</param>
        /// <returns>An decoded string.</returns>
        public virtual string DecodeString(string value, char quoteSymbol)
        {
            if (value == null) return null;

            if (value.Length >= 2 && value[0] == quoteSymbol && value[value.Length - 1] == quoteSymbol)
            {
                return value.Substring(1, value.Length - 2);
            }
            return value;
        }
    }
}
