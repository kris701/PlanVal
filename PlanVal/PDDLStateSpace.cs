﻿using PDDLSharp.Models.PDDL;
using PDDLSharp.Models.PDDL.Expressions;
using PDDLSharp.Translators.Grounders;

namespace PlanVal
{
    /// <summary>
    /// A class representing a state space in PDDL
    /// </summary>
    public class PDDLStateSpace
    {
        /// <summary>
        /// The current declaration that the space is for
        /// </summary>
        public PDDLDecl Declaration { get; internal set; }
        /// <summary>
        /// A set of state facts
        /// </summary>
        public HashSet<PredicateExp> State { get; set; }
        /// <summary>
        /// A set of goal facts
        /// </summary>
        public HashSet<PredicateExp> Goals { get; }
        internal IGrounder<IParametized> _grounder;

        internal List<PredicateExp> _tempAdd = new List<PredicateExp>();
        internal List<PredicateExp> _tempDel = new List<PredicateExp>();

        /// <summary>
        /// Main constructor
        /// </summary>
        /// <param name="declaration"></param>
        public PDDLStateSpace(PDDLDecl declaration)
        {
            Declaration = declaration;
            Goals = new HashSet<PredicateExp>();
            _grounder = new ParametizedGrounder(declaration);
            State = new HashSet<PredicateExp>();
            if (declaration.Problem.Init != null)
                foreach (var item in declaration.Problem.Init.Predicates)
                    if (item is PredicateExp predicate)
                        Add(SimplifyPredicate(predicate));
        }

        /// <summary>
        /// A copy constructor
        /// </summary>
        /// <param name="declaration"></param>
        /// <param name="currentState"></param>
        /// <param name="grounder"></param>
        public PDDLStateSpace(PDDLDecl declaration, HashSet<PredicateExp> currentState, IGrounder<IParametized> grounder)
        {
            _grounder = grounder;
            Goals = new HashSet<PredicateExp>();
            Declaration = declaration;
            State = currentState;
        }

        /// <summary>
        /// Make a deep copy of the state
        /// </summary>
        /// <returns></returns>
        public PDDLStateSpace Copy()
        {
            PredicateExp[] newState = new PredicateExp[State.Count];
            State.CopyTo(newState);
            return new PDDLStateSpace(Declaration, newState.ToHashSet(), _grounder);
        }

        public int Count => State.Count;

        private PredicateExp SimplifyPredicate(PredicateExp pred)
        {
            var copy = pred.Copy();
            copy.RemoveContext();
            copy.RemoveTypes();
            return copy;
        }

        private PredicateExp SimplifyPredicate(string predicate, params string[] arguments)
        {
            var newPred = new PredicateExp(predicate);
            newPred.Arguments = GetNameExpFromString(arguments);
            newPred.RemoveTypes();
            return newPred;
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
                newObj.RemoveContext();
                newObj.RemoveTypes();
                args.Add(newObj);
            }
            return args;
        }

        /// <summary>
        /// Add a <seealso cref="PredicateExp"/> to the state
        /// </summary>
        /// <param name="pred"></param>
        /// <returns></returns>
        public bool Add(PredicateExp pred)
        {
            if (pred.Line == -1)
                return State.Add(pred);
            return State.Add(SimplifyPredicate(pred));
        }

        /// <summary>
        /// Add a predicate by name and some arguments
        /// </summary>
        /// <param name="pred"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public bool Add(string pred, params string[] arguments) => Add(SimplifyPredicate(pred, arguments));

        /// <summary>
        /// Remove a <seealso cref="PredicateExp"/> from the state
        /// </summary>
        /// <param name="pred"></param>
        /// <returns></returns>
        public bool Del(PredicateExp pred)
        {
            if (pred.Line == -1)
                return State.Remove(pred);
            return State.Remove(SimplifyPredicate(pred));
        }

        /// <summary>
        /// Remove a predicate by name and some arguments
        /// </summary>
        /// <param name="pred"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public bool Del(string pred, params string[] arguments) => Del(SimplifyPredicate(pred, arguments));

        /// <summary>
        /// Does the state space contain a given <seealso cref="PredicateExp"/>
        /// </summary>
        /// <param name="pred"></param>
        /// <returns></returns>
        public bool Contains(PredicateExp pred)
        {
            if (pred.Line == -1)
                return State.Contains(pred);
            return State.Contains(SimplifyPredicate(pred));
        }

        /// <summary>
        /// Does the state space contain a given predicate by name and arguments.
        /// </summary>
        /// <param name="pred"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public bool Contains(string pred, params string[] arguments) => Contains(SimplifyPredicate(pred, arguments));

        /// <summary>
        /// Equals override to compare against another state
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object? obj)
        {
            if (obj is PDDLStateSpace other)
                foreach (var item in State)
                    if (!other.State.Contains(item))
                        return false;
            return true;
        }

        /// <summary>
        /// Get a hash code for the state, based on all the currently existing facts.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hash = State.Count;
            foreach (var item in State)
                hash ^= item.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Execute some <seealso cref="INode"/> on the state space. This will usually be in the form of an action
        /// </summary>
        /// <param name="node"></param>
        public virtual void ExecuteNode(INode node)
        {
            _tempAdd.Clear();
            _tempDel.Clear();
            ExecuteNode(node, false);
            foreach (var item in _tempDel)
                Del(item);
            foreach (var item in _tempAdd)
                Add(item);
        }
        internal void ExecuteNode(INode node, bool isNegative)
        {
            switch (node)
            {
                case PredicateExp predicate:
                    if (isNegative)
                        _tempDel.Add(predicate);
                    else
                        _tempAdd.Add(predicate);
                    return;
                case NotExp not:
                    ExecuteNode(not.Child, !isNegative);
                    return;
                case AndExp and:
                    foreach (var child in and.Children)
                        ExecuteNode(child, isNegative);
                    return;
                case ForAllExp all:
                    CheckPermutationsStepwise(
                    all.Expression,
                    all.Parameters,
                    (x) =>
                    {
                        ExecuteNode(x, isNegative);
                        return null;
                    });
                    return;
                case WhenExp whe:
                    if (IsNodeTrue(whe.Condition))
                        ExecuteNode(whe.Effect, false);
                    return;
                case NumericExp num:
                    return;
            }

            throw new Exception($"Unknown node type to evaluate! '{node.GetType()}'");
        }

        /// <summary>
        /// Recursively see if some <seealso cref="INode"/> is true in the state. Could be something like action precondition.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public bool IsNodeTrue(INode node)
        {
            switch (node)
            {
                case DerivedPredicateExp derivedPredicate:
                    foreach (var derivedDecl in derivedPredicate.GetDecls())
                    {
                        var newTestNode = derivedDecl.Expression.Copy();
                        for (int i = 0; i < derivedDecl.Predicate.Arguments.Count; i++)
                        {
                            var all = newTestNode.FindNames(derivedDecl.Predicate.Arguments[i].Name);
                            foreach (var name in all)
                                name.Name = derivedPredicate.Arguments[i].Name;
                        }
                        if (IsNodeTrue(newTestNode))
                            return true;
                    }
                    return false;
                case PredicateExp predicate:
                    // Handle Equality predicate
                    if (predicate.Name == "=" && predicate.Arguments.Count == 2)
                        return predicate.Arguments[0].Name == predicate.Arguments[1].Name;

                    return Contains(predicate);
                case NotExp not:
                    return !IsNodeTrue(not.Child);
                case OrExp or:
                    foreach (var subNode in or)
                        if (IsNodeTrue(subNode))
                            return true;
                    return false;
                case AndExp and:
                    foreach (var subNode in and)
                        if (!IsNodeTrue(subNode))
                            return false;
                    return true;
                case ExistsExp exist:
                    return CheckPermutationsStepwise(
                        exist.Expression,
                        exist.Parameters,
                        (x) =>
                        {
                            if (IsNodeTrue(x))
                                return true;
                            return null;
                        },
                        false);
                case ImplyExp imply:
                    if (IsNodeTrue(imply.Antecedent) && IsNodeTrue(imply.Consequent))
                        return true;
                    if (!IsNodeTrue(imply.Antecedent))
                        return true;
                    return false;
                case ForAllExp all:
                    return CheckPermutationsStepwise(
                        all.Expression,
                        all.Parameters,
                        (x) =>
                        {
                            if (!IsNodeTrue(x))
                                return false;
                            return null;
                        });
                case WhenExp whe:
                    if (IsNodeTrue(whe.Condition))
                        return IsNodeTrue(whe.Effect);
                    return false;
            }

            throw new Exception($"Unknown node type to evaluate! '{node.GetType()}'");
        }

        private bool CheckPermutationsStepwise(INode node, ParameterExp parameters, Func<INode, bool?> stopFunc, bool defaultReturn = true)
        {
            var allPermuations = _grounder.GenerateParameterPermutations(parameters.Values);
            while (allPermuations.Count > 0)
            {
                var res = stopFunc(GenerateNewParametized(node, parameters, allPermuations.Dequeue()));
                if (res != null)
                    return (bool)res;
            }
            return defaultReturn;
        }

        private INode GenerateNewParametized(INode node, ParameterExp replace, int[] with)
        {
            var checkNode = node.Copy();
            for (int i = 0; i < replace.Values.Count; i++)
            {
                var allRefs = checkNode.FindNames(replace.Values[i].Name);
                foreach (var name in allRefs)
                    name.Name = _grounder.GetObjectFromIndex(with[i]);
            }

            return checkNode;
        }

        /// <summary>
        /// Is the current state space a goal state.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public bool IsInGoal()
        {
            if (Declaration.Problem.Goal == null)
                throw new ArgumentNullException("No problem goal was declared!");
            var simplified = Declaration.Problem.Goal.GoalExp.Copy();
            simplified.RemoveContext();
            simplified.RemoveTypes();
            return IsNodeTrue(simplified);
        }
    }
}
