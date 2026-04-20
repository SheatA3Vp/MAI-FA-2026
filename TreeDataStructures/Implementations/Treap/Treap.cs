using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.Treap;

public class Treap<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, TreapNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    /// <summary>
    /// Разрезает дерево с корнем <paramref name="root"/> на два поддерева:
    /// Left: все ключи <= <paramref name="key"/>
    /// Right: все ключи > <paramref name="key"/>
    /// </summary>
    protected virtual (TreapNode<TKey, TValue>? Left, TreapNode<TKey, TValue>? Right) Split(TreapNode<TKey, TValue>? root, TKey key)
    {
        if (root is null) return (null, null);

        // 0 - совпадают, -1 - первый меньше второго, 1 - наоборот
        if (Comparer.Compare(key, root.Key) >= 0) {
            var (left, right) = Split(root.Right, key);
            root.Right = left;

            left?.Parent = root;
            root.Parent = null;
            right?.Parent = null;
            return (root, right);
        } else {
            var (left, right) = Split(root.Left, key);
            root.Left = right;

            right?.Parent = root;
            left?.Parent = null;
            root.Parent = null;
            return (left, root);
        }
    }

    /// <summary>
    /// Сливает два дерева в одно.
    /// Важное условие: все ключи в <paramref name="left"/> должны быть меньше ключей в <paramref name="right"/>.
    /// Слияние происходит на основе Priority (куча).
    /// </summary>
    protected virtual TreapNode<TKey, TValue>? Merge(TreapNode<TKey, TValue>? left, TreapNode<TKey, TValue>? right)
    {
        if (right is null) return left;
        if (left is null) return right;

        if (left.Priority > right.Priority) {
            left.Right = Merge(left.Right, right);

            left.Right?.Parent = left;
            left.Parent = null;
            return left;
        } else {
            right.Left = Merge(left, right.Left);

            right.Left?.Parent = right;
            right.Parent = null;
            return right;
        }
    }
    

    public override void Add(TKey key, TValue value)
    {
        var existingNode = FindNode(key);
        if (existingNode is not null) {
            existingNode.Value = value;
            return;
        }

        var newNode = CreateNode(key, value);

        var (left, right) = Split(this.Root, key);
        left = Merge(left, newNode);
        Root = Merge(left, right);

        ++Count;
        OnNodeAdded(newNode);
    }

    public override bool Remove(TKey key)
    {
        var toBeRemoved = FindNode(key);
        if (toBeRemoved is null) return false;

        var subtree = Merge(toBeRemoved.Left, toBeRemoved.Right);
        
        if (toBeRemoved.Parent is null) {
            Root = subtree;
        } else {
            if (toBeRemoved.IsLeftChild) {
                toBeRemoved.Parent.Left = subtree;
            } else {
                toBeRemoved.Parent.Right = subtree;
            }
        }

        if (subtree is not null) {
            subtree.Parent = toBeRemoved.Parent;
        }

        --Count;
        OnNodeRemoved(toBeRemoved.Parent, subtree);
        return true;
    }

    protected override TreapNode<TKey, TValue> CreateNode(TKey key, TValue value) => new(key, value);

    protected override void OnNodeAdded(TreapNode<TKey, TValue> newNode) { }
    
    protected override void OnNodeRemoved(TreapNode<TKey, TValue>? parent, TreapNode<TKey, TValue>? child) { }
    
}