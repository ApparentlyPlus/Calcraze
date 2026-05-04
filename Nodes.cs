using System;

public abstract class ExprNode
{
    public abstract int  Evaluate();
    public abstract string Render();
    public override string ToString() => Render();
}

// A leaf node containing a single integer value
public sealed class ValueNode : ExprNode
{
    private readonly int _value;

    // ctor
    public ValueNode(int value) => _value = value;
    public override int Evaluate() => _value;
    public override string Render() => _value.ToString();
}

// An operand node containing an operator and two child nodes
public sealed class OpNode : ExprNode
{
    public enum Operator { Add, Subtract, Multiply, Divide }
    private readonly Operator _op;
    private readonly ExprNode _left;
    private readonly ExprNode _right;

    // ctor
    public OpNode(Operator op, ExprNode left, ExprNode right)
    {
        _op    = op;
        _left  = left;
        _right = right;
    }
 
    public override int Evaluate() => _op switch
    {
        Operator.Add => _left.Evaluate() + _right.Evaluate(),
        Operator.Subtract => _left.Evaluate() - _right.Evaluate(),
        Operator.Multiply => _left.Evaluate() * _right.Evaluate(),
        Operator.Divide => _left.Evaluate() / _right.Evaluate(),

        // This never happens but just in case
        _ => throw new InvalidOperationException()
    };

    // Renders the expression as a string, adding parentheses around child nodes
    public override string Render()
    {
        char symbol = _op switch
        {
            Operator.Add => '+',
            Operator.Subtract => '-',
            Operator.Multiply => '*',
            Operator.Divide => '/',
            _ => '?'
        };
 
        // Only wrap children in parentheses when they are themselves operators
        string left  = _left is OpNode ? $"({_left.Render()})" : _left.Render();
        string right = _right is OpNode ? $"({_right.Render()})" : _right.Render();

        // Beauuuuutiful
        return $"{left} {symbol} {right}";
    }
}