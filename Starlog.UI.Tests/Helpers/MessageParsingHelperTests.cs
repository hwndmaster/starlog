using System.Collections.Immutable;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Genius.Starlog.UI.Helpers;
using Genius.Starlog.UI.Views;

namespace Genius.Starlog.UI.Tests.Helpers;

public sealed class MessageParsingHelperTests
{
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly IMessageParsingHandler _messageParsingHandlerMock;
    private readonly MessageParsingHelper _sut;

    public MessageParsingHelperTests()
    {
        _messageParsingHandlerMock = A.Fake<IMessageParsingHandler>();
        _sut = new(_messageParsingHandlerMock);
    }

    [Fact]
    public void CreateDynamicMessageParsingEntries_HappyFlowScenario()
    {
        // Arrange
        const int messageParsingsCount = 3;
        const int columnsCount = 3;
        var logItems = Enumerable.Range(1, 3).Select(_ =>
        {
            var logItem = A.Fake<ILogItemViewModel>();
            A.CallTo(() => logItem.Record).Returns(_fixture.Create<LogRecord>());
            return logItem;
        }).ToArray();
        var filterContext = LogRecordFilterContext.CreateEmpty() with {
            MessageParsings = _fixture.CreateMany<MessageParsing>(messageParsingsCount).ToImmutableArray()
        };
        List<(MessageParsing MsgParsing, string DynamicColumnName)> columnsRetrieved = [];
        Dictionary<int, string[]> logRecordsDynamicColumnValues = [];
        A.CallTo(() => _messageParsingHandlerMock.RetrieveColumns(A<MessageParsing>.Ignored))
            .ReturnsLazily((MessageParsing mp) =>
            {
                var columns = _fixture.CreateMany<string>(columnsCount).ToArray();
                columnsRetrieved.AddRange(columns.Select(c => (mp, c)));

                A.CallTo(() => _messageParsingHandlerMock.ParseMessage(mp, A<LogRecord>.Ignored, false))
                    .ReturnsLazily((MessageParsing mp, LogRecord lr, bool _) =>
                    {
                        var dynamicColumnValues = _fixture.CreateMany<string>(columnsCount).ToArray();
                        logRecordsDynamicColumnValues.Add(HashCode.Combine(mp, lr), dynamicColumnValues);
                        return dynamicColumnValues;
                    });

                return columns;
            });

        // Act
        var result = _sut.CreateDynamicMessageParsingEntries(filterContext, logItems);

        // Verify
        Assert.NotNull(result);
        Assert.False(result.HasErrors);
        Assert.Equal(columnsRetrieved.Select(x => x.DynamicColumnName), result.ColumnNames);
        Assert.Equal(columnsCount * messageParsingsCount, columnsRetrieved.Count);
        for (var i = 0; i < logItems.Length; i++)
        {
            var mpe = logItems[i].MessageParsingEntries;
            Assert.NotNull(mpe);

            for (var columnIndex = 0; columnIndex < columnsRetrieved.Count; columnIndex++)
            {
                var valuesHashCode = HashCode.Combine(columnsRetrieved[columnIndex].MsgParsing, logItems[i].Record);
                var actualValue = mpe[columnIndex];
                var expectedValue = logRecordsDynamicColumnValues[valuesHashCode][columnIndex % columnsCount];
                Assert.Equal(expectedValue, actualValue);
            }
        }
    }
}
