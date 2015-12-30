﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WebSocketStreamReaderTests.cs" company="Reimers.dk">
//   The MIT License
//   Copyright (c) 2012-2014 sta.blockhead
//   Copyright (c) 2014 Reimers.dk
//   
//   Permission is hereby granted, free of charge, to any person obtaining a copy  of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//   
//   The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//   
//   THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
// <summary>
//   Defines the WebSocketStreamReaderTests type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace WebSocketSharp.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using NUnit.Framework;

    public class WebSocketStreamReaderTests
    {
        private WebSocketStreamReaderTests()
        {
        }

        [TestFixture]
        public class GivenAWebSocketStreamReader
        {
            private WebSocketStreamReader _sut;

            [SetUp]
            public void Setup()
            {
                var data1 = Enumerable.Repeat((byte)1, 1000).ToArray();
                var frame1 = new WebSocketFrame(Fin.More, Opcode.Binary, data1, false, true);
                var data2 = Enumerable.Repeat((byte)2, 1000).ToArray();
                var frame2 = new WebSocketFrame(Fin.Final, Opcode.Cont, data2, false, true);
                var frame3 = new WebSocketFrame(Fin.Final, Opcode.Close, new byte[0], false, true);
                var stream = new MemoryStream(frame1.ToByteArray().Concat(frame2.ToByteArray()).Concat(frame3.ToByteArray()).ToArray());
                _sut = new WebSocketStreamReader(stream, 100000);
            }

            [Test]
            public async Task WhenReadingMessageThenGetsAllFrames()
            {
                var msg = await _sut.Read(CancellationToken.None).ConfigureAwait(false);

                var buffer = new byte[2000];
                var bytesRead = msg.RawData.Read(buffer, 0, 2000);

                var expected = Enumerable.Repeat((byte)1, 1000).Concat(Enumerable.Repeat((byte)2, 1000)).ToArray();
                CollectionAssert.AreEqual(expected, buffer);
            }

            [Test]
            public async Task WhenMessageDataIsNotConsumedThenDoesNotGetSecondMessage()
            {
                using (var source = new CancellationTokenSource(TimeSpan.FromSeconds(1)))
                {
                    var read = await _sut.Read(CancellationToken.None).ConfigureAwait(false);

                    Assert.Throws<OperationCanceledException>(async () => await _sut.Read(source.Token).ConfigureAwait(false));
                }
            }

            [Test]
            public async Task WhenMessageDataIsConsumedThenGetsSecondMessage()
            {
                var count = 0;
                WebSocketMessage message = null;
                while ((message = await _sut.Read(default(CancellationToken)).ConfigureAwait(false)) != null)
                {
                    message.Consume();
                    count++;
                }

                Assert.AreEqual(2, count);
            }

            [Test]
            public async Task WhenReadCalledAfterCloseFrameThenReturnsNull()
            {
                WebSocketMessage message;
                while ((message = await _sut.Read(CancellationToken.None).ConfigureAwait(false)) != null)
                {
                    message.Consume();
                }

                var second = await _sut.Read(default(CancellationToken)).ConfigureAwait(false);

                Assert.Null(second);
            }

            [Test]
            public async Task WhenReadingStreamWithPingsThenReadsAllData()
            {
                var data1 = Enumerable.Repeat((byte)1, 1000000).ToArray();
                var frame1 = new WebSocketFrame(Fin.More, Opcode.Binary, data1, false, true);
                var data2 = Enumerable.Repeat((byte)2, 1000000).ToArray();
                var frame2 = new WebSocketFrame(Fin.Final, Opcode.Cont, data2, false, true);
                var frame3 = new WebSocketFrame(Fin.Final, Opcode.Ping, new byte[0], false, true);
                var frame4 = new WebSocketFrame(Fin.Final, Opcode.Binary, data1, false, true);
                var frame5 = new WebSocketFrame(Fin.Final, Opcode.Ping, new byte[0], false, true);

                var stream = new MemoryStream(
                    frame1.ToByteArray()
                    .Concat(frame2.ToByteArray())
                    .Concat(frame3.ToByteArray())
                    .Concat(frame4.ToByteArray())
                    .Concat(frame5.ToByteArray())
                    .ToArray());
                _sut = new WebSocketStreamReader(stream, 100000);

                int messages = 0;
                WebSocketMessage message;
                while ((message = await _sut.Read(CancellationToken.None).ConfigureAwait(false)) != null)
                {
                    message.Consume();
                    messages += 1;
                }

                Assert.AreEqual(4, messages);
            }

            [Test]
            public async Task WhenReadingStreamWithCompressedFramesAndPingsThenReadsAllData()
            {
                var data1 = Enumerable.Repeat((byte)1, 1000000).ToArray();
                var frame1 = new WebSocketFrame(Fin.More, Opcode.Binary, data1.Compress(CompressionMethod.Deflate, Opcode.Binary), false, true);
                var data2 = Enumerable.Repeat((byte)2, 1000000).ToArray();
                var frame2 = new WebSocketFrame(Fin.Final, Opcode.Cont, data2.Compress(CompressionMethod.Deflate, Opcode.Binary), false, true);
                var frame3 = new WebSocketFrame(Fin.Final, Opcode.Ping, new byte[0], false, true);
                var frame4 = new WebSocketFrame(Fin.Final, Opcode.Binary, data1.Compress(CompressionMethod.Deflate, Opcode.Binary), false, true);
                var frame5 = new WebSocketFrame(Fin.Final, Opcode.Ping, new byte[0], false, true);

                var stream = new MemoryStream(
                    frame1.ToByteArray()
                    .Concat(frame2.ToByteArray())
                    .Concat(frame3.ToByteArray())
                    .Concat(frame4.ToByteArray())
                    .Concat(frame5.ToByteArray())
                    .ToArray());
                _sut = new WebSocketStreamReader(stream, 100000);

                int messages = 0;
                WebSocketMessage message;
                while ((message = await _sut.Read(CancellationToken.None).ConfigureAwait(false)) != null)
                {
                    message.Consume();
                    messages += 1;
                }


                Assert.AreEqual(4, messages);
            }
        }
    }
}