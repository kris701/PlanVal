using PDDLSharp.Models.FastDownward.Plans;
using PDDLSharp.Models.PDDL;

namespace PlanVal
{
    /// <summary>
    /// Class object for the plan validator
    /// </summary>
    public class PlanValidator
    {
        /// <summary>
        /// A number representing what step of the plan was executed last.
        /// </summary>
        public int Step { get; internal set; }
        /// <summary>
        /// If some validation error occured, it will be shown here.
        /// </summary>
        public string ValidationError { get; internal set; } = "";

        /// <summary>
        /// Validate a <seealso cref="ActionPlan"/> on a <seealso cref="PDDLDecl"/>.
        /// </summary>
        /// <param name="plan"></param>
        /// <param name="decl"></param>
        /// <returns>True if plan is valid, false otherwise</returns>
        public bool Validate(ActionPlan plan, PDDLDecl decl)
        {
            Step = 0;
            ValidationError = "";
            var simulator = new StateSpaceSimulator(decl);
            try
            {
                foreach (var step in plan.Plan)
                {
                    var argStr = new List<string>();
                    foreach (var arg in step.Arguments)
                        argStr.Add(arg.Name);

                    simulator.Step(step.ActionName, argStr.ToArray());
                    Step++;
                }
                return simulator.State.IsInGoal();
            }
            catch (Exception ex)
            {
                ValidationError = ex.Message;
                return false;
            }
        }
    }
}
