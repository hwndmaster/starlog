using System.Windows.Data;
using Genius.Atom.Infrastructure.TestingUtil;
using Genius.Atom.UI.Forms;
using Genius.Starlog.UI.Views.LogSearchAndFiltering;

namespace Genius.Starlog.UI.Tests.Views.LogSearchAndFiltering;

public sealed class LogFilterCategoryViewModelTests
{
    private readonly IFixture _fixture = InfrastructureTestHelper.CreateFixture();

    [Fact]
    public void Ctor_PropertiesInitialized()
    {
        // Arrange
        var title = _fixture.Create<string>();
        var icon = _fixture.Create<string>();
        var sort = _fixture.Create<bool>();
        var expanded = _fixture.Create<bool>();
        var canAddChildren = _fixture.Create<bool>();

        // Act
        var sut = new LogFilterCategoryViewModel<DummyLogFilterNodeViewModel>(title, icon, sort, expanded, canAddChildren);

        // Verify
        Assert.Equal(title, sut.Title);
        Assert.Equal(icon, sut.Icon);
        Assert.Equal(expanded, sut.IsExpanded);
        Assert.Equal(canAddChildren, sut.CanAddChildren);
        if (sort)
            Assert.Single(sut.CategoryItemsView.SortDescriptions);
        else
            Assert.Empty(sut.CategoryItemsView.SortDescriptions);
    }

    [Fact]
    public void AddItems_HappyFlowScenario()
    {
        // Arrange
        var sut = new LogFilterCategoryViewModel<DummyLogFilterNodeViewModel>(_fixture.Create<string>(), _fixture.Create<string>());
        var items = _fixture.Build<DummyLogFilterNodeViewModel>().OmitAutoProperties().CreateMany();

        // Act
        sut.AddItems(items);

        // Verify
        Assert.Equal(items, sut.CategoryItems);
    }

    [Fact]
    public void Remove_HappyFlowScenario()
    {
        // Arrange
        var sut = new LogFilterCategoryViewModel<DummyLogFilterNodeViewModel>(_fixture.Create<string>(), _fixture.Create<string>());
        var items = _fixture.Build<DummyLogFilterNodeViewModel>().OmitAutoProperties().CreateMany().ToArray();
        sut.AddItems(items);

        // Act
        sut.Remove(items.Skip(1));

        // Verify
        Assert.Equal([items[0]], sut.CategoryItems);
    }

    [Fact]
    public void RemoveItem_HappyFlowScenario()
    {
        // Arrange
        var sut = new LogFilterCategoryViewModel<DummyLogFilterNodeViewModel>(_fixture.Create<string>(), _fixture.Create<string>());
        var items = _fixture.Build<DummyLogFilterNodeViewModel>().OmitAutoProperties().CreateMany().ToArray();
        sut.AddItems(items);

        // Act
        sut.RemoveItem(items[1]);

        // Verify
        Assert.Equal([items[0], items[2]], sut.CategoryItems);
    }

    public sealed class DummyLogFilterNodeViewModel : ILogFilterNodeViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool CanAddChildren { get; set; }
        public bool CanModifyOrDelete { get; set; }
        public bool IsExpanded { get; set; }
        public CollectionViewSource CategoryItemsView { get; set; } = new();
        public IActionCommand AddChildCommand { get; set; } = new ActionCommand();
        public IActionCommand ModifyCommand { get; set; } = new ActionCommand();
        public IActionCommand DeleteCommand { get; set; } = new ActionCommand();
        public bool IsPinned { get; set; }
        public bool CanPin { get; set; }
    }
}
