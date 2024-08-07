﻿// ------------------------------------------------------------------------------
//  <auto-generated>
//      This code was generated by SpecFlow (https://www.specflow.org/).
//      SpecFlow Version:3.9.0.0
//      SpecFlow Generator Version:3.9.0.0
// 
//      Changes to this file may cause incorrect behavior and will be lost if
//      the code is regenerated.
//  </auto-generated>
// ------------------------------------------------------------------------------
#region Designer generated code
#pragma warning disable
namespace Genius.Starlog.Core.Tests.Comparison
{
    using TechTalk.SpecFlow;
    using System;
    using System.Linq;
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "3.9.0.0")]
    [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public partial class ComparisonServiceFeature : object, Xunit.IClassFixture<ComparisonServiceFeature.FixtureData>, System.IDisposable
    {
        
        private static TechTalk.SpecFlow.ITestRunner testRunner;
        
        private static string[] featureTags = ((string[])(null));
        
        private Xunit.Abstractions.ITestOutputHelper _testOutputHelper;
        
#line 1 "ComparisonService.feature"
#line hidden
        
        public ComparisonServiceFeature(ComparisonServiceFeature.FixtureData fixtureData, Genius_Starlog_Core_Tests_XUnitAssemblyFixture assemblyFixture, Xunit.Abstractions.ITestOutputHelper testOutputHelper)
        {
            this._testOutputHelper = testOutputHelper;
            this.TestInitialize();
        }
        
        public static void FeatureSetup()
        {
            testRunner = TechTalk.SpecFlow.TestRunnerManager.GetTestRunner();
            TechTalk.SpecFlow.FeatureInfo featureInfo = new TechTalk.SpecFlow.FeatureInfo(new System.Globalization.CultureInfo("en-US"), "Comparison", "Comparison Service", null, ProgrammingLanguage.CSharp, featureTags);
            testRunner.OnFeatureStart(featureInfo);
        }
        
        public static void FeatureTearDown()
        {
            testRunner.OnFeatureEnd();
            testRunner = null;
        }
        
        public void TestInitialize()
        {
        }
        
        public void TestTearDown()
        {
            testRunner.OnScenarioEnd();
        }
        
        public void ScenarioInitialize(TechTalk.SpecFlow.ScenarioInfo scenarioInfo)
        {
            testRunner.OnScenarioInitialize(scenarioInfo);
            testRunner.ScenarioContext.ScenarioContainer.RegisterInstanceAs<Xunit.Abstractions.ITestOutputHelper>(_testOutputHelper);
        }
        
        public void ScenarioStart()
        {
            testRunner.OnScenarioStart();
        }
        
        public void ScenarioCleanup()
        {
            testRunner.CollectScenarioErrors();
        }
        
        void System.IDisposable.Dispose()
        {
            this.TestTearDown();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="Combines 4x4 records, keeping only first and last similar records matched:", Skip="Ignored")]
        [Xunit.TraitAttribute("FeatureTitle", "Comparison Service")]
        [Xunit.TraitAttribute("Description", "Combines 4x4 records, keeping only first and last similar records matched:")]
        public void Combines4X4RecordsKeepingOnlyFirstAndLastSimilarRecordsMatched()
        {
            string[] tagsOfScenario = new string[] {
                    "ignore"};
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Combines 4x4 records, keeping only first and last similar records matched:", null, tagsOfScenario, argumentsOfScenario, featureTags);
#line 4
  this.ScenarioInitialize(scenarioInfo);
#line hidden
            if ((TagHelper.ContainsIgnoreTag(tagsOfScenario) || TagHelper.ContainsIgnoreTag(featureTags)))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
                TechTalk.SpecFlow.Table table1 = new TechTalk.SpecFlow.Table(new string[] {
                            "DateTime",
                            "Level",
                            "Thread",
                            "File",
                            "Logger",
                            "Message"});
                table1.AddRow(new string[] {
                            "2023-01-01 12:00:00.001",
                            "INFO",
                            "Main",
                            "file2.log",
                            "Logger1",
                            "Message 123"});
                table1.AddRow(new string[] {
                            "2023-01-01 12:00:00.123",
                            "INFO",
                            "Thread2",
                            "file1.log",
                            "Logger1",
                            "Another Message"});
                table1.AddRow(new string[] {
                            "2023-01-01 12:00:01.376",
                            "INFO",
                            "Thread3",
                            "file1.log",
                            "Logger1",
                            "Yet Another Message"});
                table1.AddRow(new string[] {
                            "2023-01-01 12:00:01.465",
                            "INFO",
                            "Thread4",
                            "file1.log",
                            "Logger2",
                            "Final Message"});
#line 5
    testRunner.Given("log records from profile 1:", ((string)(null)), table1, "Given ");
#line hidden
                TechTalk.SpecFlow.Table table2 = new TechTalk.SpecFlow.Table(new string[] {
                            "DateTime",
                            "Level",
                            "Thread",
                            "File",
                            "Logger",
                            "Message"});
                table2.AddRow(new string[] {
                            "2023-01-01 12:30:00.001",
                            "INFO",
                            "Main",
                            "file2.log",
                            "Logger1",
                            "Message 123"});
                table2.AddRow(new string[] {
                            "2023-01-01 12:30:00.123",
                            "INFO",
                            "Thread2",
                            "file1.log",
                            "Logger1",
                            "Another Message Second Profile"});
                table2.AddRow(new string[] {
                            "2023-01-01 12:30:00.376",
                            "INFO",
                            "Thread3",
                            "file1.log",
                            "Logger1",
                            "Yet Another Message Second Profile"});
                table2.AddRow(new string[] {
                            "2023-01-01 12:30:00.465",
                            "INFO",
                            "Thread4",
                            "file1.log",
                            "Logger2",
                            "Final Message"});
#line 11
    testRunner.And("log records from profile 2:", ((string)(null)), table2, "And ");
#line hidden
#line 17
    testRunner.When("comparing profiles", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
                TechTalk.SpecFlow.Table table3 = new TechTalk.SpecFlow.Table(new string[] {
                            "Record1",
                            "Record2"});
                table3.AddRow(new string[] {
                            "1",
                            "1"});
                table3.AddRow(new string[] {
                            "2",
                            ""});
                table3.AddRow(new string[] {
                            "3",
                            ""});
                table3.AddRow(new string[] {
                            "",
                            "2"});
                table3.AddRow(new string[] {
                            "",
                            "3"});
                table3.AddRow(new string[] {
                            "4",
                            "4"});
#line 18
    testRunner.Then("the result is the following:", ((string)(null)), table3, "Then ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="Combines 3x2 records", Skip="Ignored")]
        [Xunit.TraitAttribute("FeatureTitle", "Comparison Service")]
        [Xunit.TraitAttribute("Description", "Combines 3x2 records")]
        public void Combines3X2Records()
        {
            string[] tagsOfScenario = new string[] {
                    "ignore"};
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Combines 3x2 records", null, tagsOfScenario, argumentsOfScenario, featureTags);
#line 28
  this.ScenarioInitialize(scenarioInfo);
#line hidden
            if ((TagHelper.ContainsIgnoreTag(tagsOfScenario) || TagHelper.ContainsIgnoreTag(featureTags)))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
                TechTalk.SpecFlow.Table table4 = new TechTalk.SpecFlow.Table(new string[] {
                            "DateTime",
                            "Level",
                            "Thread",
                            "File",
                            "Logger",
                            "Message"});
                table4.AddRow(new string[] {
                            "2023-01-01 12:00:00.001",
                            "INFO",
                            "Main",
                            "file2.log",
                            "Logger1",
                            "Message 123"});
                table4.AddRow(new string[] {
                            "2023-01-01 12:00:00.123",
                            "INFO",
                            "Thread2",
                            "file1.log",
                            "Logger1",
                            "Another Message"});
                table4.AddRow(new string[] {
                            "2023-01-01 12:00:03.465",
                            "INFO",
                            "Thread4",
                            "file1.log",
                            "Logger2",
                            "Final Message"});
#line 29
    testRunner.Given("log records from profile 1:", ((string)(null)), table4, "Given ");
#line hidden
                TechTalk.SpecFlow.Table table5 = new TechTalk.SpecFlow.Table(new string[] {
                            "DateTime",
                            "Level",
                            "Thread",
                            "File",
                            "Logger",
                            "Message"});
                table5.AddRow(new string[] {
                            "2023-01-01 12:30:00.001",
                            "INFO",
                            "Main",
                            "file2.log",
                            "Logger1",
                            "Message 123"});
                table5.AddRow(new string[] {
                            "2023-01-01 12:30:00.555",
                            "INFO",
                            "Thread2",
                            "file1.log",
                            "Logger1",
                            "Another Message"});
#line 34
    testRunner.And("log records from profile 2:", ((string)(null)), table5, "And ");
#line hidden
#line 38
    testRunner.When("comparing profiles", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
                TechTalk.SpecFlow.Table table6 = new TechTalk.SpecFlow.Table(new string[] {
                            "Record1",
                            "Record2"});
                table6.AddRow(new string[] {
                            "1",
                            "1"});
                table6.AddRow(new string[] {
                            "2",
                            "2"});
                table6.AddRow(new string[] {
                            "3",
                            ""});
#line 39
    testRunner.Then("the result is the following:", ((string)(null)), table6, "Then ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [Xunit.SkippableFactAttribute(DisplayName="Combines 2x3 records", Skip="Ignored")]
        [Xunit.TraitAttribute("FeatureTitle", "Comparison Service")]
        [Xunit.TraitAttribute("Description", "Combines 2x3 records")]
        public void Combines2X3Records()
        {
            string[] tagsOfScenario = new string[] {
                    "ignore"};
            System.Collections.Specialized.OrderedDictionary argumentsOfScenario = new System.Collections.Specialized.OrderedDictionary();
            TechTalk.SpecFlow.ScenarioInfo scenarioInfo = new TechTalk.SpecFlow.ScenarioInfo("Combines 2x3 records", null, tagsOfScenario, argumentsOfScenario, featureTags);
#line 46
  this.ScenarioInitialize(scenarioInfo);
#line hidden
            if ((TagHelper.ContainsIgnoreTag(tagsOfScenario) || TagHelper.ContainsIgnoreTag(featureTags)))
            {
                testRunner.SkipScenario();
            }
            else
            {
                this.ScenarioStart();
                TechTalk.SpecFlow.Table table7 = new TechTalk.SpecFlow.Table(new string[] {
                            "DateTime",
                            "Level",
                            "Thread",
                            "File",
                            "Logger",
                            "Message"});
                table7.AddRow(new string[] {
                            "2023-01-01 12:00:00.001",
                            "INFO",
                            "Main",
                            "file2.log",
                            "Logger1",
                            "Message 123"});
                table7.AddRow(new string[] {
                            "2023-01-01 12:00:00.123",
                            "INFO",
                            "Thread2",
                            "file1.log",
                            "Logger1",
                            "Another Message"});
#line 47
    testRunner.Given("log records from profile 1:", ((string)(null)), table7, "Given ");
#line hidden
                TechTalk.SpecFlow.Table table8 = new TechTalk.SpecFlow.Table(new string[] {
                            "DateTime",
                            "Level",
                            "Thread",
                            "File",
                            "Logger",
                            "Message"});
                table8.AddRow(new string[] {
                            "2023-01-01 12:30:00.001",
                            "INFO",
                            "Main",
                            "file2.log",
                            "Logger1",
                            "Message 123"});
                table8.AddRow(new string[] {
                            "2023-01-01 12:30:00.555",
                            "INFO",
                            "Thread2",
                            "file1.log",
                            "Logger1",
                            "Another Message"});
                table8.AddRow(new string[] {
                            "2023-01-01 12:30:03.465",
                            "INFO",
                            "Thread4",
                            "file1.log",
                            "Logger2",
                            "Final Message"});
#line 51
    testRunner.And("log records from profile 2:", ((string)(null)), table8, "And ");
#line hidden
#line 56
    testRunner.When("comparing profiles", ((string)(null)), ((TechTalk.SpecFlow.Table)(null)), "When ");
#line hidden
                TechTalk.SpecFlow.Table table9 = new TechTalk.SpecFlow.Table(new string[] {
                            "Record1",
                            "Record2"});
                table9.AddRow(new string[] {
                            "1",
                            "1"});
                table9.AddRow(new string[] {
                            "2",
                            "2"});
                table9.AddRow(new string[] {
                            "",
                            "3"});
#line 57
    testRunner.Then("the result is the following:", ((string)(null)), table9, "Then ");
#line hidden
            }
            this.ScenarioCleanup();
        }
        
        [System.CodeDom.Compiler.GeneratedCodeAttribute("TechTalk.SpecFlow", "3.9.0.0")]
        [System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
        public class FixtureData : System.IDisposable
        {
            
            public FixtureData()
            {
                ComparisonServiceFeature.FeatureSetup();
            }
            
            void System.IDisposable.Dispose()
            {
                ComparisonServiceFeature.FeatureTearDown();
            }
        }
    }
}
#pragma warning restore
#endregion
