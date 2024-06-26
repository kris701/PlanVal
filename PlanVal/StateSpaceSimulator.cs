﻿using PDDLSharp.Contextualisers;
using PDDLSharp.Contextualisers.PDDL;
using PDDLSharp.ErrorListeners;
using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Domain;
using PDDLSharp.Models.PDDL.Expressions;

namespace PlanVal
{
    /// <summary>
    /// A small simulator to simulate PDDL action executions
    /// </summary>
    public class StateSpaceSimulator
    {
        /// <summary>
        /// The current <seealso cref="PDDLDecl"/> that is simulated on
        /// </summary>
        public PDDLDecl Declaration { get; internal set; }
        /// <summary>
        /// An object representing the state
        /// </summary>
        public PDDLStateSpace State { get; internal set; }
        /// <summary>
        /// The current cost of the simulated run
        /// </summary>
        public int Cost { get; internal set; } = 0;

        /// <summary>
        /// Main constructor
        /// </summary>
        /// <param name="declaration"></param>
        public StateSpaceSimulator(PDDLDecl declaration)
        {
            Declaration = declaration;
            if (!Declaration.IsContextualised)
            {
                IErrorListener listener = new ErrorListener(ParseErrorType.Error);
                IContextualiser contextualiser = new PDDLContextualiser(listener);
                contextualiser.Contexturalise(Declaration);
            }

            State = new PDDLStateSpace(declaration);
        }

        /// <summary>
        /// Set the cost to zero and reset the state space object.
        /// </summary>
        public void Reset()
        {
            Cost = 0;
            State = new PDDLStateSpace(Declaration);
        }

        /// <summary>
        /// Execute a action by name with a set of arguments
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="arguments"></param>
        public void Step(string actionName, params string[] arguments) => Step(actionName, GetNameExpFromString(arguments));

        /// <summary>
        /// Execute a action by name with no arguments.
        /// </summary>
        /// <param name="actionName"></param>
        public void Step(string actionName) => Step(actionName, new List<NameExp>());

        private void Step(string actionName, List<NameExp> arguments)
        {
            actionName = actionName.ToLower();

            var targetAction = GetTargetAction(actionName).Copy();
            targetAction.RemoveContext();

            if (targetAction.Parameters.Values.Count != arguments.Count)
                throw new ArgumentOutOfRangeException($"Action '{targetAction.Name}' takes '{targetAction.Parameters.Values.Count}' arguments, but was given '{arguments.Count}'");

            targetAction = GroundAction(targetAction, arguments);

            if (!State.IsNodeTrue(targetAction.Preconditions))
                throw new ArgumentException($"Not all precondition predicates are set for the target action '{targetAction.Name}'!");

            State.ExecuteNode(targetAction.Effects);

            Cost++;
        }

        private ActionDecl GetTargetAction(string actionName)
        {
            var targetAction = Declaration.Domain.Actions.FirstOrDefault(x => x.Name == actionName);
            if (targetAction == null)
                throw new ArgumentNullException($"Could not find an action called '{actionName}'");
            return targetAction;
        }

        private ActionDecl GroundAction(ActionDecl node, List<NameExp> groundArgs)
        {
            for (int i = 0; i < node.Parameters.Values.Count; i++)
            {
                if (!groundArgs[i].Type.IsTypeOf(node.Parameters.Values[i].Type.Name))
                    throw new ArgumentException($"Given argument type is incorrect, expected a '{node.Parameters.Values[i].Type.Name}' but got a '{groundArgs[i].Type.Name}'");

                var names = node.FindNames(node.Parameters.Values[i].Name);
                foreach (var name in names)
                    name.Name = groundArgs[i].Name;
            }
            node.RemoveContext();
            node.RemoveTypes();
            return node;
        }

        private List<NameExp> GetNameExpFromString(string[] arguments)
        {
            var args = new List<NameExp>();
            foreach (var arg in arguments)
            {
                NameExp? obj = null;
                if (Declaration.Problem.Objects != null)
                    obj = Declaration.Problem.Objects.Objs.FirstOrDefault(x => x.Name == arg.ToLower());
                if (obj == null && Declaration.Domain.Constants != null)
                    obj = Declaration.Domain.Constants.Constants.FirstOrDefault(x => x.Name == arg.ToLower());

                if (obj == null)
                    throw new ArgumentException($"Cannot find object (or constant) '{arg}'");
                var newObj = obj.Copy();
                args.Add(newObj);
            }
            return args;
        }
    }
}
