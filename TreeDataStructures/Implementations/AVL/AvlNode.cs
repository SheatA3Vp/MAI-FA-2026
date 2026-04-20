using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlNode<TKey, TValue>(TKey key, TValue value)
    : Node<TKey, TValue, AvlNode<TKey, TValue>>(key, value)
{
    // Разница между высотами правого и левого поддеревьев
    public int Diff { get; set; } = 0;
}