using CommandLine.Text;
using CommandLine;
using PDDLSharp.CodeGenerators.FastDownward.Plans;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Problem;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Parsers.PDDL;
using PDDLSharp.Translators;
using PDDLSharp.Parsers.FastDownward.Plans;
using PDDLSharp.Models.FastDownward.Plans;

namespace PlanVal.CLI
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var parser = new Parser(with => with.HelpWriter = null);
            var parserResult = parser.ParseArguments<Options>(args);
            parserResult.WithNotParsed(errs => DisplayHelp(parserResult, errs));
            parserResult.WithParsed(Run);
        }

        public static void Run(Options opts)
        {
            opts.DomainPath = RootPath(opts.DomainPath);
            opts.ProblemPath = RootPath(opts.ProblemPath);
            opts.PlanPath = RootPath(opts.PlanPath);

            Console.WriteLine("Parsing files...");
            var listener = new ErrorListener();
            var pddlParser = new PDDLParser(listener);
            var domain = pddlParser.ParseAs<DomainDecl>(new FileInfo(opts.DomainPath));
            var problem = pddlParser.ParseAs<ProblemDecl>(new FileInfo(opts.ProblemPath));
            var pddlDecl = new PDDLDecl(domain, problem);
            var planParser = new FDPlanParser(listener);
            var plan = planParser.ParseAs<ActionPlan>(new FileInfo(opts.PlanPath));

            Console.WriteLine("Validating...");
            var validator = new PlanValidator();
            var result = validator.Validate(plan, pddlDecl);

            if (result)
                Console.WriteLine("== PLAN IS VALID ==");
            else
            {
                Console.WriteLine("== PLAN IS INVALID ==");
                Console.WriteLine($"Reason: {validator.ValidationError}");
            }
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
            var sentenceBuilder = SentenceBuilder.Create();
            foreach (var error in errs)
                if (error is not HelpRequestedError)
                    Console.WriteLine(sentenceBuilder.FormatError(error));
        }

        private static void DisplayHelp<T>(ParserResult<T> result, IEnumerable<Error> errs)
        {
            var helpText = HelpText.AutoBuild(result, h =>
            {
                h.AddEnumValuesToHelpText = true;
                return h;
            }, e => e, verbsIndex: true);
            Console.WriteLine(helpText);
            HandleParseError(errs);
        }

        private static string RootPath(string path)
        {
            if (!Path.IsPathRooted(path))
                path = Path.Join(Directory.GetCurrentDirectory(), path);
            path = path.Replace("\\", "/");
            return path;
        }
    }
}