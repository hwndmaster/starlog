using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogFiltering;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;

namespace Genius.Starlog.Core.Tests.LogFiltering;

public sealed class FieldFilterProcessorTests
{
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture(useMutableValueTypeGenerator: true);

    [Theory]
    [InlineData(false, "1|2|3", "2", true)]
    [InlineData(false, "1|2|3", "4", false)]
    [InlineData(true, "1|2|3", "2", false)]
    [InlineData(true, "1|2|3", "4", true)]
    public void IsMatch_Scenarios(bool exclude, string valueFilters, string valueToMatch, bool expected)
    {
        // Arrange
        const int fieldId = 0;
        string[] fieldValues = ["1", "2", "3", "4"];
        var fieldsMock = new Mock<ILogFieldsContainer>();
        fieldsMock.Setup(x => x.GetFieldValue(fieldId, It.IsAny<int>()))
            .Returns((int fieldId, int fieldValueId) => fieldValues[fieldValueId]);
        var logContainer = Mock.Of<ILogContainer>(x => x.GetFields() == fieldsMock.Object);
        var sut = new FieldFilterProcessor(logContainer);
        var profileFilter = new FieldProfileFilter(_fixture.Create<LogFilter>())
        {
            FieldId = fieldId,
            Values = valueFilters.Split('|'),
            Exclude = exclude
        };
        var logRecord = _fixture.Build<LogRecord>()
            .With(x => x.FieldValueIndices, [Array.IndexOf(fieldValues, valueToMatch)])
            .Create();

        // Act
        var actual = sut.IsMatch(profileFilter, logRecord);

        // Verify
        Assert.Equal(expected, actual);
    }
}
