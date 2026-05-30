using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using UnityEditor;
using UnityEngine;

namespace KHorrorGame.EditorTools
{
    public static class KHorrorInternalPlayflowReport
    {
        private const string ReportPath = "Artifacts/Reports/internal-playflow-proof.md";
        private const string ScreenshotPath = "Artifacts/Screenshots/internal-playflow-proof.png";

        [MenuItem("Tools/K Horror Migration/Generate Internal Playflow Report")]
        public static void GenerateInternalPlayflowProofReport()
        {
            var projectRoot = Directory.GetCurrentDirectory();
            var reportPath = Path.Combine(projectRoot, ReportPath);
            var screenshotPath = Path.Combine(projectRoot, ScreenshotPath);
            var testResultsPath = FindLatestTestResultsPath(projectRoot);
            var branch = RunGit(projectRoot, "rev-parse --abbrev-ref HEAD");
            var commitSha = RunGit(projectRoot, "rev-parse --short HEAD");

            GenerateInternalPlayflowProofReportForTest(
                reportPath,
                testResultsPath,
                screenshotPath,
                branch,
                commitSha);

            UnityEngine.Debug.Log("Saved internal playflow proof report to: " + reportPath);
        }

        public static void GenerateInternalPlayflowProofReportForTest(
            string reportPath,
            string testResultsPath,
            string screenshotPath,
            string branch,
            string commitSha)
        {
            if (string.IsNullOrWhiteSpace(reportPath))
            {
                throw new ArgumentException("Report path is required.", nameof(reportPath));
            }

            if (string.IsNullOrWhiteSpace(testResultsPath) || !File.Exists(testResultsPath))
            {
                throw new FileNotFoundException("Test results XML is required.", testResultsPath);
            }

            if (string.IsNullOrWhiteSpace(screenshotPath) || !File.Exists(screenshotPath))
            {
                throw new FileNotFoundException("Internal playflow screenshot is required.", screenshotPath);
            }

            var summary = ReadTestSummary(testResultsPath);
            var screenshot = new FileInfo(screenshotPath);
            var reportDirectory = Path.GetDirectoryName(reportPath);
            if (!string.IsNullOrEmpty(reportDirectory))
            {
                Directory.CreateDirectory(reportDirectory);
            }

            File.WriteAllText(
                reportPath,
                BuildReport(
                    Normalize(branch, "unknown"),
                    Normalize(commitSha, "unknown"),
                    summary,
                    screenshot),
                new UTF8Encoding(false));
        }

        private static string BuildReport(
            string branch,
            string commitSha,
            TestSummary summary,
            FileInfo screenshot)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Internal Playflow Proof");
            builder.AppendLine();
            builder.AppendLine("## Source");
            builder.AppendLine();
            builder.AppendLine("- Branch: `" + branch + "`");
            builder.AppendLine("- Commit: `" + commitSha + "`");
            builder.AppendLine("- Generated: `" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " UTC`");
            builder.AppendLine();
            builder.AppendLine("## Verification");
            builder.AppendLine();
            builder.AppendLine("- Tests: `" + summary.Total + " total / " + summary.Passed + " passed / " + summary.Failed + " failed / " + summary.Skipped + " skipped`");
            builder.AppendLine("- Test result: `" + summary.Result + "`");
            builder.AppendLine("- Screenshot: `" + screenshot.Name + "`");
            builder.AppendLine("- Screenshot bytes: `" + screenshot.Length + "`");
            builder.AppendLine();
            builder.AppendLine("## Covered Flow");
            builder.AppendLine();
            builder.AppendLine("- Core loop smoke: `BongoHub -> JonggaEstate -> cargo load -> re-pickup -> return -> settlement`");
            builder.AppendLine("- Threat loop smoke: `shrine theft -> grace -> ghost actor -> audio occlusion -> atmosphere cue`");
            builder.AppendLine("- Cargo drop proof: `single-owner G input`, `inside van -> cargo hold`, `outside van -> world pickup`, `lowered-floor drop snap`");
            builder.AppendLine("- Screenshot proof: bongo terminal, cargo hold, held cargo, shrine threat cue.");
            return builder.ToString();
        }

        private static TestSummary ReadTestSummary(string testResultsPath)
        {
            var document = new XmlDocument();
            document.Load(testResultsPath);
            var run = document.SelectSingleNode("//test-run");
            if (run == null)
            {
                throw new InvalidDataException("Test results XML does not contain a test-run node: " + testResultsPath);
            }

            return new TestSummary(
                AttributeInt(run, "total"),
                AttributeInt(run, "passed"),
                AttributeInt(run, "failed"),
                AttributeInt(run, "skipped"),
                AttributeString(run, "result", "Unknown"));
        }

        private static string FindLatestTestResultsPath(string projectRoot)
        {
            var rootCandidate = Directory.EnumerateFiles(projectRoot, "TestResults-InternalPlayflow*.xml", SearchOption.TopDirectoryOnly)
                .Select(path => new FileInfo(path))
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .FirstOrDefault();

            if (rootCandidate != null)
            {
                return rootCandidate.FullName;
            }

            var localLowPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "..",
                "LocalLow",
                "DefaultCompany",
                "KHorrorUnity",
                "TestResults.xml");
            return Path.GetFullPath(localLowPath);
        }

        private static string RunGit(string workingDirectory, string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo("git", arguments)
                {
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        return "unknown";
                    }

                    var output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit(5000);
                    return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output) ? output : "unknown";
                }
            }
            catch (Exception)
            {
                return "unknown";
            }
        }

        private static int AttributeInt(XmlNode node, string name)
        {
            int value;
            return int.TryParse(AttributeString(node, name, "0"), out value) ? value : 0;
        }

        private static string AttributeString(XmlNode node, string name, string fallback)
        {
            var attribute = node.Attributes != null ? node.Attributes[name] : null;
            return attribute != null ? attribute.Value : fallback;
        }

        private static string Normalize(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private readonly struct TestSummary
        {
            public TestSummary(int total, int passed, int failed, int skipped, string result)
            {
                Total = total;
                Passed = passed;
                Failed = failed;
                Skipped = skipped;
                Result = result;
            }

            public int Total { get; }
            public int Passed { get; }
            public int Failed { get; }
            public int Skipped { get; }
            public string Result { get; }
        }
    }
}
