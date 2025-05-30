﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Liversen.DependencyCop
{
    /// <summary>
    /// Directed acyclic graph.
    /// </summary>
    class Dag
    {
        readonly Dictionary<string, Dependencies> nodes = new Dictionary<string, Dependencies>();

        public ImmutableDictionary<string, ImmutableHashSet<string>> DirectVertices() =>
            nodes.ToImmutableDictionary(x => x.Key, x => x.Value.Direct.ToImmutableHashSet());

        public ImmutableDictionary<string, ImmutableHashSet<string>> TransitiveVertices() =>
            nodes.ToImmutableDictionary(x => x.Key, x => x.Value.Transitive.ToImmutableHashSet());

        public void AddVertex(string source, string target)
        {
            var cycle = TryAddVertex(source, target);
            if (cycle != null)
            {
                throw new ArgumentException($"Cycle detected: {cycle}");
            }
        }

        /// <summary>
        /// Tries to add a vertex.
        /// </summary>
        /// <param name="source">Source vertex.</param>
        /// <param name="target">Target vertex.</param>
        /// <returns>If vertex was added, null is returned, otherwise a list of nodes forming a cycle.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1168:Empty arrays and collections should be returned instead of null", Justification = "It is more efficient to return null and efficiency matters here.")]
        public ImmutableArray<string>? TryAddVertex(string source, string target)
        {
            var sourceDependencies = GetDependencies(source);
            if (sourceDependencies.Direct.Contains(target))
            {
                return null;
            }

            var targetDependencies = GetDependencies(target);
            if (targetDependencies.Transitive.Contains(source))
            {
                var list = new List<string>();
                var current = target;
                while (current != source)
                {
                    list.Add(current);
                    current = nodes[current].Direct.First(x => x == source || nodes[x].Transitive.Contains(source));
                }

                list.Add(source);
                list.Add(target);
                return list.ToImmutableArray();
            }

            sourceDependencies.Direct.Add(target);
            foreach (var dependencies in nodes.Where(x => x.Key == source || (x.Key != target && x.Value.Transitive.Contains(source))).Select(x => x.Value.Transitive))
            {
                dependencies.Add(target);
                dependencies.UnionWith(targetDependencies.Transitive);
            }

            return null;
        }

        Dependencies GetDependencies(string node)
        {
            if (nodes.TryGetValue(node, out var existingTargets))
            {
                return existingTargets;
            }

            var targets = new Dependencies();
            nodes.Add(node, targets);
            return targets;
        }

        class Dependencies
        {
            public HashSet<string> Direct { get; } = new HashSet<string>();

            public HashSet<string> Transitive { get; } = new HashSet<string>();
        }
    }
}
