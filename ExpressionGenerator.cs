
using System;
using System.Collections.Generic;
using System.Linq;

// The big boy class, contains the magic of generating random yet solvable expressions :D
public class ExpressionGenerator
{
    // The finest of in house RNGs, Donald E. Knuth's subtractive algo
    private readonly Random _rng;
    
    // ctor
    public ExpressionGenerator(int? seed = null) =>
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
 
    // Generates a random expression tree that evaluates to an integer in [config.TargetMin, config.TargetMax]
    public ExprNode Generate(ExpressionConfig config)
    {
        int target = _rng.Next(config.TargetMin, config.TargetMax + 1);
        return Build(config.MaxDepth, target, config);
    }
 
    // Core builder
    private ExprNode Build(int depth, int target, ExpressionConfig config)
    {
        // Produce a leaf when we've exhausted depth or a random roll says so
        bool isLeaf = depth == 0 || (depth < config.MaxDepth && _rng.NextDouble() < config.LeafBias);
 
        if (isLeaf)
        {
            // Clamp the target to the allowed leaf range
            // When AllowNegative is false we never let a leaf be negative

            int minLeaf = config.AllowNegative ? -config.LeafMax : config.LeafMin;
            int clamped = Math.Clamp(target, minLeaf, config.LeafMax);
            return new ValueNode(clamped);
        }
 
        // Pick a random operator from the enabled set
        var op = PickOperator(target, config);
 
        // Decompose the target according to the chosen operator, then recurse
        var (leftTarget, rightTarget) = Decompose(op, target, config);
 
        ExprNode left = Build(depth - 1, leftTarget,  config);
        ExprNode right = Build(depth - 1, rightTarget, config);
 
        return new OpNode(op, left, right);
    }
 
    // Op selection
    private OpNode.Operator PickOperator(int target, ExpressionConfig config)
    {
        // Build a weighted pool of candidates
        // Some operators are only valid for certain targets 
        // for example, multiplication needs a factorizable target

        var pool = new List<(OpNode.Operator op, int weight)>();
 
        if (config.AddWeight > 0) pool.Add((OpNode.Operator.Add, config.AddWeight));
        if (config.SubtractWeight > 0) pool.Add((OpNode.Operator.Subtract, config.SubtractWeight));
 
        // Multiply is only useful when target has at least one non trivial factor
        if (config.MultiplyWeight > 0 && GetFactors(target, config).Count > 0)
            pool.Add((OpNode.Operator.Multiply, config.MultiplyWeight));
 
        // dividend = target * divisor so that we have a clean int
        if (config.DivideWeight > 0)
            pool.Add((OpNode.Operator.Divide, config.DivideWeight));
 
        if (pool.Count == 0)
            return OpNode.Operator.Add; // This should rearly (if ever) happen
 
        int total = pool.Sum(p => p.weight);
        int roll = _rng.Next(total);
        int cursor = 0;
 
        foreach (var (op, weight) in pool)
        {
            cursor += weight;
            if (roll < cursor) return op;
        }

        // Beautiful C# syntax right here, never fails
        return pool[^1].op;
    }

    // Given an operator and target result, returns (leftTarget, rightTarget)
    // so that left OP right = target is guaranteed
    private (int left, int right) Decompose(OpNode.Operator op, int target, ExpressionConfig config)
    {
        int minLeaf = config.AllowNegative ? -config.LeafMax : config.LeafMin;
        int maxLeaf = config.LeafMax;
 
        switch (op)
        {
            case OpNode.Operator.Add:
            {
                // left + right = target, pick left, derive right.
                int left = _rng.Next(minLeaf, maxLeaf + 1);
                int right = target - left;
                return (left, right);
            }
 
            case OpNode.Operator.Subtract:
            {
                // vice vers from add
                int right = _rng.Next(config.LeafMin, maxLeaf + 1);
                int left = target + right;
                return (left, right);
            }
 
            case OpNode.Operator.Multiply:
            {
                // left * right = target, pick a factor as right, derive left
                var factors = GetFactors(target, config);
                if (factors.Count == 0) goto case OpNode.Operator.Add;
                int right = factors[_rng.Next(factors.Count)];
                int left  = target / right;
                return (left, right);
            }
 
            case OpNode.Operator.Divide:
            {
                // left / right = target, pick right (divisor), left = target * right
                int right = _rng.Next(config.LeafMin, maxLeaf + 1);
                int left  = target * right;
                return (left, right);
            }
 
            default:
                throw new InvalidOperationException($"Unknown operator: {op}");
        }
    }
  
    // Self explanatory, returns a list of valid factors of target within the leaf range
    private static List<int> GetFactors(int target, ExpressionConfig config)
    {
        var factors = new List<int>();
        if (target == 0) return factors;

        int absTarget = Math.Abs(target);
        int limit = (int)Math.Sqrt(absTarget);

        // Check factors up to the square root
        for (int i = 1; i <= limit; i++)
        {
            if (absTarget % i == 0)
            {
                // Potential factor 1 -> i
                // Potential factor 2 -> absTarget / i
                CheckAndAdd(i, target, config, factors);
                
                int complementary = absTarget / i;
                if (complementary != i)
                {
                    CheckAndAdd(complementary, target, config, factors);
                }
            }
        }
        return factors;
    }

    // Helper for GetFactors to check if a factor (or its negative) is valid and add it to the list
    private static void CheckAndAdd(int factor, int target, ExpressionConfig config, List<int> list)
    {
        // Check positive version
        if (factor >= config.LeafMin && factor <= config.LeafMax && target % factor == 0)
            list.Add(factor);

        // Check negative version
        if (config.AllowNegative)
        {
            int negFactor = -factor;
            if (negFactor >= config.LeafMin && 
                    negFactor <= config.LeafMax && target % negFactor == 0)
                list.Add(negFactor);
        }
    }

}