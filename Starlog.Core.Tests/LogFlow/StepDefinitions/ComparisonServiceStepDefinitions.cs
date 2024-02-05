using System.Collections.Immutable;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Starlog.Core.LogFlow;
using Genius.Starlog.Core.Models;
using Genius.Starlog.Core.TestingUtil;
using TechTalk.SpecFlow;

namespace Genius.Starlog.Core.Tests.LogFlow;

[Binding]
public sealed class ComparisonServiceStepDefinitions
{
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture();
    private readonly TestProfileLoaderFactory _profileLoaderFactory = new();
    private readonly ComparisonService _sut;

    private readonly ScenarioContext _scenarioContext;

    public ComparisonServiceStepDefinitions(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;

        _sut = new ComparisonService(_profileLoaderFactory);
    }

    [Given("log records from profile (.*):")]
    public void GivenLogRecordsFromProfileX(int profileIndex, Table table)
    {
        var profile = new Profile
        {
            Name = _fixture.Create<string>(),
            Settings = _fixture.Create<ProfileSettingsBase>()
        };

        var records = table.Rows.Select(row => new LogRecord(
            DateTimeOffset.Parse(row[0]),
            new LogLevelRecord(row[1].GetHashCode(), row[0]),
            row[2],
            new FileRecord(_fixture.Create<string>() + @"\" + row[3], _fixture.Create<long>()),
            new LoggerRecord(row[4].GetHashCode(), row[4]),
            row[5],
            null)).ToImmutableArray();

        var profileLoaderMock = new Mock<IProfileLoader>();
        _profileLoaderFactory.InstanceToReturn = profileLoaderMock.Object;

        profileLoaderMock.Setup(x => x.LoadProfileAsync(profile, It.IsAny<ILogContainerWriter>()))
            .Callback((Profile profile, ILogContainerWriter logContainerWriter) =>
            {
                logContainerWriter.AddLogs(records);

                var sources = records.Select(x => x.Source).DistinctBy(x => x.DisplayName);
                foreach (var source in sources)
                {
                    logContainerWriter.AddSource(source);
                }
            })
            // TODO: create a proper FilesBasedProfileState
            .ReturnsAsync(_fixture.Create<ProfileStateBase>());

        _scenarioContext.Add("Profile" + profileIndex, profile);
    }

    [When("comparing profiles")]
    public async Task WhenComparingProfiles()
    {
        var profile1 = (Profile)_scenarioContext["Profile1"];
        var profile2 = (Profile)_scenarioContext["Profile2"];

        var result = await _sut.LoadProfilesAsync(profile1, profile2);
        _scenarioContext.Add("Result", result);
    }

    [Then("the result is the following:")]
    public void ThenTheResultIsTheFollowing(Table table)
    {
        var result = (ComparisonContext)_scenarioContext["Result"];

        Assert.Equal(table.RowCount, result.Records.Length);

        var refRecords1 = result.LogContainer1.GetLogs();
        var refRecords2 = result.LogContainer2.GetLogs();

        for (var i = 0; i < result.Records.Length; i++)
        {
            var record = result.Records[i];
            var row = table.Rows[i];
            var expectedRecordIndex1 = row[0];
            var expectedRecordIndex2 = row[1];

            if (!string.IsNullOrEmpty(expectedRecordIndex1))
            {
                Assert.Equal(refRecords1[int.Parse(expectedRecordIndex1) - 1], record.Record1);
            }
            else
            {
                Assert.Null(record.Record1);
            }
            if (!string.IsNullOrEmpty(expectedRecordIndex2))
            {
                Assert.Equal(refRecords2[int.Parse(expectedRecordIndex2) - 1], record.Record2);
            }
            else
            {
                Assert.Null(record.Record2);
            }
        }
    }
}
