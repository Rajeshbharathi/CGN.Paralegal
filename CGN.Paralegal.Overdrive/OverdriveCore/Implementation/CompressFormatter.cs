using System;
using System.Messaging;
using System.IO.Compression;
using System.IO;
using System.Threading;

namespace LexisNexis.Evolution.Overdrive.MSMQ
{
    using System.Text;

    /// <summary>
    /// CompressFormatter - Formatter used to send compressed Messages
    /// </summary>
    public class CompressFormatter : IMessageFormatter
    {
        private IMessageFormatter m_BaseFormatter = null;
       
        /// <summary>
        /// CompressFormatter constructor
        /// </summary>
        /// <param name="BaseFormatter">The based formatter - used to serialize the object before compression and after decompression</param>
        public CompressFormatter(IMessageFormatter BaseFormatter)
        {
            m_BaseFormatter = BaseFormatter;
        }

        #region IMessageFormatter Members

        /// <summary>
        /// Determines wheather the formatter can deserialize the message
        /// </summary>
        /// <param name="message">The message to inspect</param>
        /// <returns>true if the message can be serialized. false otherwise</returns>
        public bool CanRead(Message message)
        {
            return (message.BodyStream != null);
        }

        private class MessageHeader
        {
            internal MessageHeader(long uncompressedBodyLength)
            {
                this.uncompressedBodyLength = uncompressedBodyLength;
            }

            internal MessageHeader(Stream stream)
            {
                byte[] magicPrefixBuffer = new byte[unicodeEncoding.GetByteCount(CompressionMagicPrefix)];
                stream.Read(magicPrefixBuffer, 0, magicPrefixBuffer.Length);
                string bodyPrefix = unicodeEncoding.GetString(magicPrefixBuffer);
                PrefixIsValid = CompressionMagicPrefix.Equals(bodyPrefix, StringComparison.Ordinal);

                if (PrefixIsValid)
                {
                    byte[] bodyLenBuffer = new byte[sizeof(Int32)];
                    stream.Read(bodyLenBuffer, 0, bodyLenBuffer.Length);
                    uncompressedBodyLength = BitConverter.ToInt32(bodyLenBuffer, 0);
                }
            }

            private const string CompressionMagicPrefix = "ZIP!";
            private static readonly UnicodeEncoding unicodeEncoding = new UnicodeEncoding();

            internal bool PrefixIsValid = false;

            private readonly long uncompressedBodyLength;
            internal long UncompressedBodyLength
            {
                get
                {
                    return uncompressedBodyLength;
                }
            }

            internal void CopyTo(Stream stream)
            {
                // Write the prefix indicating compressed body
                stream.Write(unicodeEncoding.GetBytes(CompressionMagicPrefix), 0, unicodeEncoding.GetByteCount(CompressionMagicPrefix));

                // Write the length of the original body (not compressed) before the compressed body
                stream.Write(BitConverter.GetBytes((Int32)uncompressedBodyLength), 0, sizeof(Int32));
            }

            internal static int Length
            {
                get
                {
                    return unicodeEncoding.GetByteCount(CompressionMagicPrefix) + sizeof(Int32);
                }
            }
        }

        private static ThreadLocal<long> _originalBodyLength = new ThreadLocal<long>(() => 0); 
        public long OriginalBodyLength
        {
            get
            {
                return _originalBodyLength.Value;
            } 
            private set
            {
                _originalBodyLength.Value = value;
            }
        }

        private static ThreadLocal<long> _compressedBodyLength = new ThreadLocal<long>(() => 0);
        public long CompressedBodyLength
        {
            get
            {
                return _compressedBodyLength.Value;
            }
            private set
            {
                _compressedBodyLength.Value = value;
            }
        }

        /// <summary>
        /// Reads a message. Decomress and then applies the base formatter
        /// </summary>
        /// <param name="message">The message to be read</param>
        /// <returns>The result object - goes to the message Body</returns>
        public object Read(Message message)
        {
            long bodyLength = message.BodyStream.Length;
            OriginalBodyLength = bodyLength;
            CompressedBodyLength = bodyLength;

            if (bodyLength < MessageHeader.Length)
            {
                return m_BaseFormatter.Read(message); // Message appeared to be uncompressed 
            }

            MessageHeader messageHeader = new MessageHeader(message.BodyStream);

            if (!messageHeader.PrefixIsValid)
            {
                message.BodyStream.Position = 0;
                return m_BaseFormatter.Read(message); // Message appeared to be uncompressed 
            }

            OriginalBodyLength = messageHeader.UncompressedBodyLength;
            using (MemoryStream decompressedBodyMemoryStream = new MemoryStream())
            {
                DeflateStream decompressor = new DeflateStream(message.BodyStream, CompressionMode.Decompress);
                decompressor.CopyTo(decompressedBodyMemoryStream);
                decompressor.Close();

                // Create a "clear" message and apply the base format to it
                Message clearMessage = new Message();
                clearMessage.BodyType = message.BodyType;
                clearMessage.BodyStream = decompressedBodyMemoryStream;
                clearMessage.BodyStream.Position = 0;

                return m_BaseFormatter.Read(clearMessage);
            }
        }

        /// <summary>
        /// Writes an object to the message's body. First applies the base formatter, then compresses.
        /// </summary>
        /// <param name="message">The message to be written to</param>
        /// <param name="obj">The message to write</param>
        public void Write(Message message, object obj)
        {
            Message clearMessage = new Message();
            m_BaseFormatter.Write(clearMessage, obj);

            message.BodyType = clearMessage.BodyType;

            // In case the original body length is less than 1 kilobyte - don't compress it
            long uncompressedBodyLength = clearMessage.BodyStream.Length;
            if (uncompressedBodyLength <= 1024)
            {
                message.BodyStream = clearMessage.BodyStream;
                message.BodyStream.Position = 0;
                return;
            }

            MemoryStream compressedBodyMemoryStream = new MemoryStream();
            using (DeflateStream compressor = new DeflateStream(compressedBodyMemoryStream, CompressionMode.Compress, true))
            {
                MessageHeader messageHeader = new MessageHeader(uncompressedBodyLength);
                messageHeader.CopyTo(compressedBodyMemoryStream); // Header goes uncompressed!

                clearMessage.BodyStream.Position = 0;
                clearMessage.BodyStream.CopyTo(compressor);
            }

            long compressedBodyLength = compressedBodyMemoryStream.Length;
            if (compressedBodyLength < uncompressedBodyLength)
            {
                message.BodyStream = compressedBodyMemoryStream;
                message.BodyStream.Position = 0;
            }
            else
            {
                message.BodyStream = clearMessage.BodyStream;
                message.BodyStream.Position = 0;
            }
        }

        #endregion

        #region ICloneable Members

        /// <summary>
        /// Makes a copy of the formatter
        /// </summary>
        /// <returns>A copy of the formatter</returns>
        public object Clone()
        {
            return new CompressFormatter((IMessageFormatter) m_BaseFormatter.Clone());
        }

        #endregion
    }
}
