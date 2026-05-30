using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace KHorrorGame.Migration.Tests
{
    public sealed class InternalPlayFlowReportTests
    {
        [Test]
        public void ReportGeneratorWritesBranchCommitTestsAndScreenshotSummary()
        {
            var tempRoot = Path.Combine(Application.temporaryCachePath, "internal-playflow-report-test");
            Directory.CreateDirectory(tempRoot);

            var reportPath = Path.Combine(tempRoot, "internal-playflow-proof.md");
            var testResultsPath = Path.Combine(tempRoot, "TestResults-InternalPlayflow.xml");
            var screenshotPath = Path.Combine(tempRoot, "internal-playflow-proof.png");

            File.WriteAllText(
                testResultsPath,
                "<test-run total=\"100\" passed=\"100\" failed=\"0\" skipped=\"0\" result=\"Passed\" />");
            File.WriteAllBytes(screenshotPath, new byte[24576]);

            if (File.Exists(reportPath))
            {
                File.Delete(reportPath);
            }

            var reportType = FindLoadedType("KHorrorGame.EditorTools.KHorrorInternalPlayflowReport");
            Assert.IsNotNull(reportType, "Internal playflow report editor type should exist.");

            var method = reportType.GetMethod(
                "GenerateInternalPlayflowProofReportForTest",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "Report generator should expose a deterministic test entry point.");

            method.Invoke(null, new object[]
            {
                reportPath,
                testResultsPath,
                screenshotPath,
                "codex/test-branch",
                "abc1234"
            });

            Assert.IsTrue(File.Exists(reportPath), "Report markdown should be written.");
            var report = File.ReadAllText(reportPath);

            StringAssert.Contains("# Internal Playflow Proof", report);
            StringAssert.Contains("Branch: `codex/test-branch`", report);
            StringAssert.Contains("Commit: `abc1234`", report);
            StringAssert.Contains("Tests: `100 total / 100 passed / 0 failed / 0 skipped`", report);
            StringAssert.Contains("Screenshot: `internal-playflow-proof.png`", report);
            StringAssert.Contains("Screenshot bytes: `24576`", report);
            StringAssert.Contains("BongoHub -> JonggaEstate -> cargo load -> re-pickup -> return -> settlement", report);
            StringAssert.Contains("shrine theft -> grace -> ghost actor -> audio occlusion -> atmosphere cue", report);
            StringAssert.Contains("single-owner G input", report);
            StringAssert.Contains("inside van -> cargo hold", report);
            StringAssert.Contains("outside van -> world pickup", report);
            StringAssert.Contains("lowered-floor drop snap", report);
        }

        private static Type FindLoadedType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
