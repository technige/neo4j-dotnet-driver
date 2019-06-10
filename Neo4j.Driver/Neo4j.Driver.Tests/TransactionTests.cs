﻿// Copyright (c) 2002-2019 "Neo4j,"
// Neo4j Sweden AB [http://neo4j.com]
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

using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Neo4j.Driver.Internal;
using Neo4j.Driver.Internal.Connector;
using Neo4j.Driver.Internal.Protocol;
using Neo4j.Driver;
using Xunit;
using static Neo4j.Driver.Tests.SessionTests;
using static Xunit.Record;

namespace Neo4j.Driver.Tests
{
    public class TransactionTests
    {
        public class Constructor
        {
            [Fact]
            public void ShouldObtainProtocolFromConnection()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new Transaction(mockConn.Object, new SyncExecutor());

                mockConn.Verify(x => x.BoltProtocol);
            }

            [Fact]
            public async Task ShouldSaveBookmark()
            {
                var mockConn = new Mock<IConnection>();
                var mockProtocol = new Mock<IBoltProtocol>();
                mockConn.Setup(x => x.BoltProtocol).Returns(mockProtocol.Object);

                var bookmark = Bookmark.From(FakeABookmark(123));
                var tx = new Transaction(mockConn.Object, new SyncExecutor(), null, null, bookmark);

                await tx.BeginTransactionAsync(null);
                mockProtocol.Verify(
                    x => x.BeginTransactionAsync(It.IsAny<IConnection>(), bookmark, It.IsAny<TransactionConfig>()),
                    Times.Once);
            }
        }

        public class BeginTransactionMethod
        {
            [Fact]
            public void ShouldDelegateToProtocolBeginTxMethod()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new Transaction(mockConn.Object, new SyncExecutor());

                tx.BeginTransaction(TransactionConfig.Empty);

                protocol.Verify(
                    x => x.BeginTransactionAsync(It.IsAny<IConnection>(), It.IsAny<Bookmark>(),
                        It.IsAny<TransactionConfig>()), Times.Once);
            }
        }

        public class BeginTransactionAsyncMethod
        {
            [Fact]
            public async Task ShouldDelegateToProtocolBeginTxMethod()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new Transaction(mockConn.Object, new SyncExecutor());

                await tx.BeginTransactionAsync(TransactionConfig.Empty);
                protocol.Verify(
                    x => x.BeginTransactionAsync(It.IsAny<IConnection>(), It.IsAny<Bookmark>(),
                        It.IsAny<TransactionConfig>()), Times.Once);
            }
        }

        public class RunMethod
        {
            [Theory]
            [InlineData(true)]
            [InlineData(false)]
            public void ShouldDelegateToBoltProtocol(bool reactive)
            {
                var protocol = new Mock<IBoltProtocol>();
                protocol.Setup(x =>
                        x.RunInExplicitTransactionAsync(It.IsAny<IConnection>(), It.IsAny<Statement>(), reactive))
                    .ReturnsAsync(new Mock<IStatementResultCursor>().Object);

                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new Transaction(mockConn.Object, new SyncExecutor(), reactive: reactive);

                var statement = new Statement("lalala");
                tx.Run(statement);

                protocol.Verify(x => x.RunInExplicitTransactionAsync(It.IsAny<IConnection>(), statement, reactive),
                    Times.Once);
            }

            [Fact]
            public void ShouldThrowExceptionIfPreviousTxFailed()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new Transaction(mockConn.Object, new SyncExecutor());
                tx.MarkToClose();

                var error = Exception(() => tx.Run("ttt"));
                error.Should().BeOfType<ClientException>();
            }

            [Fact]
            public void ShouldThrowExceptionIfFailedToRunAndFetchResult()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new Transaction(mockConn.Object, new SyncExecutor());
                var statement = new Statement("lala");

                protocol.Setup(x =>
                        x.RunInExplicitTransactionAsync(It.IsAny<IConnection>(), statement, It.IsAny<bool>()))
                    .Returns(TaskHelper.GetFailedTask<IStatementResultCursor>(new Neo4jException()));

                var error = Exception(() => tx.Run(statement));
                error.Should().BeOfType<Neo4jException>();
            }
        }

        public class RunAsyncMethod
        {
            [Fact]
            public async void ShouldDelegateToBoltProtocol()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new Transaction(mockConn.Object, new SyncExecutor());

                var statement = new Statement("lala");
                await tx.RunAsync(statement);

                protocol.Verify(x => x.RunInExplicitTransactionAsync(It.IsAny<IConnection>(), statement, false));
            }

            [Fact]
            public async void ShouldThrowExceptionIfPreviousTxFailed()
            {
                var mockConn = new Mock<IConnection>();
                var tx = new Transaction(mockConn.Object, new SyncExecutor());
                tx.MarkToClose();

                var error = await ExceptionAsync(() => tx.RunAsync("ttt"));
                error.Should().BeOfType<ClientException>();
            }

            [Fact]
            public async void ShouldThrowExceptionIfFailedToRunAndFetchResult()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new Transaction(mockConn.Object, new SyncExecutor());
                var statement = new Statement("lala");

                protocol.Setup(x => x.RunInExplicitTransactionAsync(It.IsAny<IConnection>(), statement, false))
                    .Throws<Neo4jException>();

                var error = await ExceptionAsync(() => tx.RunAsync(statement));
                error.Should().BeOfType<Neo4jException>();
            }
        }

        public class DisposeMethod
        {
            [Fact]
            public void ShouldCommitOnSuccess()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var mockHandler = new Mock<ITransactionResourceHandler>();
                var tx = new Transaction(mockConn.Object, new SyncExecutor(), mockHandler.Object);

                mockConn.ResetCalls();
                tx.Success();
                tx.Dispose();

                protocol.Verify(x => x.CommitTransactionAsync(It.IsAny<IConnection>(), tx), Times.Once);
                mockHandler.Verify(x => x.OnTransactionDisposeAsync(It.IsAny<Bookmark>()), Times.Once);
            }

            [Fact]
            public void ShouldRollbackOnFailure()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var mockHandler = new Mock<ITransactionResourceHandler>();
                var tx = new Transaction(mockConn.Object, new SyncExecutor(), mockHandler.Object);

                mockConn.ResetCalls();
                tx.Success();
                // Even if success is called, but if failure is called afterwards, then we rollback
                tx.Failure();
                tx.Dispose();

                protocol.Verify(x => x.RollbackTransactionAsync(It.IsAny<IConnection>()), Times.Once);
                mockHandler.Verify(x => x.OnTransactionDisposeAsync(It.IsAny<Bookmark>()), Times.Once);
            }

            [Fact]
            public void ShouldRollbackOnNoExplicitSuccess()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var mockHandler = new Mock<ITransactionResourceHandler>();
                var tx = new Transaction(mockConn.Object, new SyncExecutor(), mockHandler.Object);

                mockConn.ResetCalls();
                tx.Dispose();
                protocol.Verify(x => x.RollbackTransactionAsync(It.IsAny<IConnection>()), Times.Once);
                mockHandler.Verify(x => x.OnTransactionDisposeAsync(It.IsAny<Bookmark>()), Times.Once);
            }

            [Fact]
            public void ShouldNotReturnConnectionToPoolTwice()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var mockHandler = new Mock<ITransactionResourceHandler>();
                var tx = new Transaction(mockConn.Object, new SyncExecutor(), mockHandler.Object);

                mockConn.ResetCalls();
                tx.Dispose();
                tx.Dispose();
                mockHandler.Verify(x => x.OnTransactionDisposeAsync(It.IsAny<Bookmark>()), Times.Once);
            }
        }

        public class CloseAsyncMethod
        {
            [Fact]
            public async void ShouldCommitOnSuccess()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var mockHandler = new Mock<ITransactionResourceHandler>();
                var tx = new Transaction(mockConn.Object, new SyncExecutor(), mockHandler.Object);

                mockConn.ResetCalls();
                await tx.CommitAsync();

                protocol.Verify(x => x.CommitTransactionAsync(It.IsAny<IConnection>(), tx));
                mockHandler.Verify(x => x.OnTransactionDisposeAsync(It.IsAny<Bookmark>()), Times.Once);
            }

            [Fact]
            public async void ShouldRollbackOnFailure()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var mockHandler = new Mock<ITransactionResourceHandler>();
                var tx = new Transaction(mockConn.Object, new SyncExecutor(), mockHandler.Object);

                mockConn.ResetCalls();
                await tx.RollbackAsync();
                protocol.Verify(x => x.RollbackTransactionAsync(It.IsAny<IConnection>()));
                mockHandler.Verify(x => x.OnTransactionDisposeAsync(It.IsAny<Bookmark>()), Times.Once);
            }

            [Fact]
            public async void ShouldNotReturnConnectionToPoolTwice()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var mockHandler = new Mock<ITransactionResourceHandler>();
                var tx = new Transaction(mockConn.Object, new SyncExecutor(), mockHandler.Object);

                mockConn.ResetCalls();
                await tx.CommitAsync();
                await tx.RollbackAsync();
                protocol.Verify(x => x.CommitTransactionAsync(It.IsAny<IConnection>(), tx));
                protocol.Verify(x => x.RollbackTransactionAsync(It.IsAny<IConnection>()), Times.Never);
                mockHandler.Verify(x => x.OnTransactionDisposeAsync(It.IsAny<Bookmark>()), Times.Once);
            }

            [Fact]
            public async void ShouldNotDisposeIfAlreadyClosed()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var mockHandler = new Mock<ITransactionResourceHandler>();
                var tx = new Transaction(mockConn.Object, new SyncExecutor(), mockHandler.Object);

                mockConn.ResetCalls();
                await tx.CommitAsync();
                tx.Dispose();
                mockHandler.Verify(x => x.OnTransactionDisposeAsync(It.IsAny<Bookmark>()), Times.Once);
            }
        }

        public class MarkToClosedMethod
        {
            [Fact]
            public void ShouldNotEnqueueMoreMessagesAfterMarkToClosed()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new Transaction(mockConn.Object, new SyncExecutor());
                mockConn.ResetCalls();

                tx.MarkToClose();
                tx.Dispose();

                protocol.Verify(x => x.RollbackTransactionAsync(It.IsAny<IConnection>()), Times.Never);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }

            [Fact]
            public void ShouldThrowExceptionToRunAfterMarkToClosed()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new Transaction(mockConn.Object, new SyncExecutor());
                mockConn.ResetCalls();

                tx.MarkToClose();

                var exception = Exception(() => tx.Run("should not run"));
                exception.Should().BeOfType<ClientException>();
                exception.Message.Should().StartWith("Cannot run more statements in this transaction");

                protocol.Verify(x => x.RollbackTransactionAsync(It.IsAny<IConnection>()), Times.Never);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }

            [Fact]
            public async Task ShouldNotEnqueueMoreMessagesAfterMarkToClosedInCommitAsync()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new Transaction(mockConn.Object, new SyncExecutor());
                mockConn.ResetCalls();

                tx.MarkToClose();
                await tx.CommitAsync();
                protocol.Verify(x => x.CommitTransactionAsync(It.IsAny<IConnection>(), tx), Times.Never);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }

            [Fact]
            public async Task ShouldNotEnqueueMoreMessagesAfterMarkToClosedInRollbackAsync()
            {
                var protocol = new Mock<IBoltProtocol>();
                var mockConn = NewMockedConnection(protocol.Object);
                var tx = new Transaction(mockConn.Object, new SyncExecutor());
                mockConn.ResetCalls();

                tx.MarkToClose();
                await tx.RollbackAsync();
                protocol.Verify(x => x.RollbackTransactionAsync(It.IsAny<IConnection>()), Times.Never);
                mockConn.Verify(x => x.SyncAsync(), Times.Never);
            }
        }
    }
}