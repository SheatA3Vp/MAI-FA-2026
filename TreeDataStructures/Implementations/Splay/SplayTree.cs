using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
    where TKey : IComparable<TKey>
{
    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    protected void Splay(BstNode<TKey, TValue>? node)
    {
        if (node == null) return;

        while (node.Parent != null)
        {
            var parent = node.Parent;
            var grandparent = parent.Parent;

            if (grandparent == null)
            {
                // zig, деда нет, родитель - корень
                if (node.IsLeftChild) {
                    RotateRight(parent);
                } else {
                    RotateLeft(parent);
                }
            }
            else if (node.IsLeftChild && parent.IsLeftChild)
            {
                // zig-zig (оба левые дети)
                RotateRight(grandparent);
                RotateRight(parent);
            }
            else if (!node.IsLeftChild && !parent.IsLeftChild)
            {
                // zig-zig (оба правые дети)
                RotateLeft(grandparent);
                RotateLeft(parent);
            }
            else if (node.IsLeftChild && !parent.IsLeftChild)
            {
                // zig-zag (узел - левый, родитель - правый)
                RotateRight(parent);
                RotateLeft(grandparent);
            }
            else
            {
                // zig-zag (узел - правый, родитель - левый)
                RotateLeft(parent);
                RotateRight(grandparent);
            }
        }
        
        Root = node;
    }
    
    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }
    
    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child) { }
    
    protected override BstNode<TKey, TValue>? FindNode(TKey key)
    {
        var current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) {
                Splay(current);
                return current; 
            }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

}
