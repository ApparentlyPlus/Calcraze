public class ExpressionConfig
{
    // How deeply the tree can nest. 0 = single number, 1 = a+b, 2 = (a+b)*c
    public int MaxDepth { get; set; } = 2;
 
    // The result of the top level expression will be in [TargetMin, TargetMax]
    public int TargetMin { get; set; } = 1;
    public int TargetMax { get; set; } = 20;
 
    // Leaf node values are clamped to [LeafMin, LeafMax]
    public int LeafMin { get; set; } = 1;
    public int LeafMax { get; set; } = 12;
 
    // Allows negative intermediate values and leaf nodes
    public bool AllowNegative { get; set; } = false;
 
    // Relative weights for how likely the generator is to use each operator
    public int AddWeight      { get; set; } = 3;
    public int SubtractWeight { get; set; } = 3;
    public int MultiplyWeight { get; set; } = 2;
    public int DivideWeight   { get; set; } = 1;
 
    // Probability that the generator produces a leaf even when MaxDepth
    // has not been reached yet. The higher the shallower the tree.
    public double LeafBias { get; set; } = 0.25;
 
    // Some presetz because we are cool kidz
 
    public static ExpressionConfig Easy => new()
    {
        MaxDepth = 2, TargetMin = 1, TargetMax = 18,
        LeafMin = 1, LeafMax = 10,
        AllowNegative = false,
        AddWeight = 3, SubtractWeight = 3, MultiplyWeight = 0, DivideWeight = 0,
        LeafBias = 0.6
    };

    public static ExpressionConfig Medium => new()
    {
        MaxDepth = 2, TargetMin = 2, TargetMax = 45,
        LeafMin = 1, LeafMax = 12,
        AllowNegative = false,
        AddWeight = 3, SubtractWeight = 3, MultiplyWeight = 1, DivideWeight = 1,
        LeafBias = 0.25
    };

    public static ExpressionConfig Balanced => Medium;

    public static ExpressionConfig Hard => new()
    {
        MaxDepth = 3, TargetMin = -20, TargetMax = 90,
        LeafMin = 1, LeafMax = 15,
        AllowNegative = true,
        AddWeight = 2, SubtractWeight = 2, MultiplyWeight = 2, DivideWeight = 1,
        LeafBias = 0.18
    };

    // Utility to create a copy of the config for presets so that 
    // we don't accidentally modify the static instances
    public ExpressionConfig Copy() => new()
    {
        MaxDepth = MaxDepth,
        TargetMin = TargetMin,
        TargetMax = TargetMax,
        LeafMin = LeafMin,
        LeafMax = LeafMax,
        AllowNegative = AllowNegative,
        AddWeight = AddWeight,
        SubtractWeight = SubtractWeight,
        MultiplyWeight = MultiplyWeight,
        DivideWeight = DivideWeight,
        LeafBias = LeafBias,
    };
}