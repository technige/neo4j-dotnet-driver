﻿// Copyright (c) 2002-2018 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Neo4j.Driver.Internal.IO.StructHandlers;
using Neo4j.Driver.Internal.Messaging;
using Neo4j.Driver.V1;

namespace Neo4j.Driver.Internal.IO
{
    internal class BoltReader: IBoltReader
    {
        private readonly IChunkReader _chunkReader;
        private readonly IPackStreamReader _packStreamReader;
        private readonly ILogger _logger;
        private readonly MemoryStream _bufferStream;
        private readonly int _defaultBufferSize;
        private readonly int _maxBufferSize;

        private int _shrinkCounter = 0;

        public BoltReader(Stream stream, IPackStreamFactory packStreamFactory)
            : this(stream, Constants.DefaultReadBufferSize, Constants.MaxReadBufferSize, null, packStreamFactory)
        {

        }

        public BoltReader(Stream stream, int defaultBufferSize, int maxBufferSize, ILogger logger, IPackStreamFactory packStreamFactory)
            : this(new ChunkReader(stream, logger), defaultBufferSize, maxBufferSize, logger, packStreamFactory)
        {

        }

        public BoltReader(IChunkReader chunkReader, int defaultBufferSize, int maxBufferSize, ILogger logger, IPackStreamFactory packStreamFactory)
        {
            Throw.ArgumentNullException.IfNull(chunkReader, nameof(chunkReader));
            Throw.ArgumentNullException.IfNull(packStreamFactory, nameof(packStreamFactory));

            _logger = logger;
            _chunkReader = chunkReader;
            _defaultBufferSize = defaultBufferSize;
            _maxBufferSize = maxBufferSize;
            _bufferStream = new MemoryStream(_defaultBufferSize);
            _packStreamReader = packStreamFactory.CreateReader(_bufferStream);
        }

        public void Read(IMessageResponseHandler responseHandler)
        {
            var messages = _chunkReader.ReadNextMessages(_bufferStream);

            ConsumeMessages(responseHandler, messages);
        }

        public Task ReadAsync(IMessageResponseHandler responseHandler)
        {
            return
                _chunkReader.ReadNextMessagesAsync(_bufferStream)
                    .ContinueWith(t =>
                    {
                        ConsumeMessages(responseHandler, t.Result);
                    }, TaskContinuationOptions.ExecuteSynchronously);
        }

        private void ConsumeMessages(IMessageResponseHandler responseHandler, int messages)
        {
            var leftMessages = messages;

            while (_bufferStream.Length > _bufferStream.Position && leftMessages > 0)
            {
                ProcessMessage(responseHandler);

                leftMessages -= 1;
            }

            // Check whether we have incomplete message in the buffers
            if (_bufferStream.Length == _bufferStream.Position)
            {
                _bufferStream.SetLength(0);

                if (_bufferStream.Capacity > _maxBufferSize)
                {
                    _logger?.Info(
                        $@"Shrinking read buffers to the default read buffer size {
                                _defaultBufferSize
                            } since its size reached {
                                _bufferStream.Capacity
                            } which is larger than the maximum read buffer size {
                                _maxBufferSize
                            }. This has already occurred {_shrinkCounter} times for this connection.");

                    _shrinkCounter += 1;

                    _bufferStream.Capacity = _defaultBufferSize;
                }
            }
        }

        private void ProcessMessage(IMessageResponseHandler responseHandler)
        {
            var message = _packStreamReader.Read();

            if (message is IResponseMessage response)
            {
                response.Dispatch(responseHandler);
            }
            else
            {
                throw new ProtocolException($"Unknown response message type {message.GetType().FullName}");
            }
        }

    }
}
